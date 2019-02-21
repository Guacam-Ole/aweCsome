using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AweCsome.Attributes.FieldAttributes;
using AweCsome.Attributes.IgnoreAttributes;
using AweCsome.Attributes.TableAttributes;
using AweCsome.Entities;
using AweCsome.Exceptions;
using AweCsome.Interfaces;
using AweCsome.Interfaces;
using log4net;
using Microsoft.SharePoint.Client;
using File = Microsoft.SharePoint.Client.File;

namespace AweCsome
{
    public class AweCsomeTable : IAweCsomeTable
    {
        private ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IAweCsomeField _awecsomeField = new AweCsomeField();
        private IAweCsomeTaxonomy _awecsomeTaxonomy = null;
        private ClientContext _clientContext;

        public AweCsomeTable(ClientContext clientContext)
        {
            _clientContext = clientContext;
        }

        //     public ClientContext ClientContext { set { _clientContext = value; } }

        #region Helpers

        private ClientContext GetClientContext()
        {
            if (_clientContext == null) throw new MissingFieldException("Please provide a valid ClientContext");
            return _clientContext;
        }

        private int? GetTableDocumentTemplateType(Type entityType)
        {
            var descriptionAttribute = entityType.GetCustomAttribute<DocumentTemplateTypeAttribute>();
            return descriptionAttribute?.DocumentTemplateTypeId;
        }

        private void AssignPropertiesToListItem<T>(T entity, ListItem listItem)
        {
            Type entityType = typeof(T);
            foreach (var property in entityType.GetProperties())
            {
                try
                {
                    if (!property.CanRead) continue;
                    var ignoreAttribute = property.GetCustomAttribute<IgnoreOnInsertAttribute>();
                    if (ignoreAttribute != null && ignoreAttribute.IgnoreOnInsert) continue;
                    var value = EntityHelper.GetItemValueFromProperty(property, entity);
                    if (property.PropertyType == typeof(DateTime))
                    {
                        var year = ((DateTime)value).Year;
                        if (year < 1900 || year > 8900) throw new ArgumentOutOfRangeException("SharePoint-Datetime must be within 1900 and 8900");
                    }
                    if (value != null) listItem[EntityHelper.GetInternalNameFromProperty(property)] = value;
                }
                catch (Exception ex)
                {
                    ex.Data.Add("Propertyname", property.Name);
                    ex.Data.Add("Listname", listItem);
                    throw (ex);
                }
            }
        }

        private string GetTableUrl(Type entityType)
        {
            var descriptionAttribute = entityType.GetCustomAttribute<Attributes.TableAttributes.TableUrlAttribute>();
            return descriptionAttribute?.Url;
        }

        private QuickLaunchOptions? GetQuickLaunchOption(Type entityType)
        {
            var descriptionAttribute = entityType.GetCustomAttribute<QuickLaunchOptionAttribute>();
            return descriptionAttribute?.QuickLaunchOption;
        }

        private Dictionary<string, Guid> GetLookupTableIds(ClientContext clientContext, Type entityType)
        {
            var lookupTableIds = new Dictionary<string, Guid>();

            foreach (var property in entityType.GetProperties())
            {
                string listname = AweCsomeField.GetLookupListName(property, out string fieldname);
                if (!string.IsNullOrWhiteSpace(listname) && !lookupTableIds.ContainsKey(listname))
                {
                    lookupTableIds.Add(listname, Guid.Empty);
                }
            }

            foreach (var listname in lookupTableIds.Keys.ToList())
            {
                List lookupList = clientContext.Web.Lists.GetByTitle(listname);
                clientContext.Load(lookupList, l => l.Id);
                clientContext.ExecuteQuery();
                lookupTableIds[listname] = lookupList.Id;
            }

            return lookupTableIds;
        }

        private void ValidateBeforeListCreation(ClientContext clientContext, string listName)
        {
            Web web = clientContext.Web;
            var listCollection = web.Lists;
            clientContext.Load(listCollection);
            clientContext.ExecuteQuery();

            var oldList = listCollection.FirstOrDefault(lst => lst.Title == listName);
            if (oldList != null)
            {
                _log.Warn($"List '{listName}' already exists. Will not create");
                throw new ItemAlreadyExistsException($"List {listName} already exists");
            }
        }

        private Folder GetFolderFromDocumentLibrary<T>(ClientContext context, string foldername)
        {
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));
            Web web = context.Web;
            string folderUrl = $"{listname}\\{foldername}";
            var folder = web.GetFolderByServerRelativeUrl(folderUrl);

            if (folder == null) return null;
            try
            {
                context.Load(folder, f => f.Exists);
                context.ExecuteQuery();
                if (!folder.Exists) return null;
            }
            catch
            {
                return null; // There is no cleaner way to do this on CSOM sadly
            }
            return folder;
        }

        private FileCollection GetAttachments(string listname, int id)
        {
            using (var clientContext = GetClientContext())
            {
                Web web = clientContext.Web;
                var targetUrl = string.Format("{0}/Lists/{1}/Attachments/{2}", web.Url, listname, id);

                Folder attachmentsFolder = web.GetFolderByServerRelativeUrl(targetUrl);
                clientContext.Load(attachmentsFolder);

                try
                {
                    clientContext.ExecuteQuery();
                }
                catch (Exception)
                {
                    // Sadly there is no better way to detect if attachments exist in SharePoint. Exception=No Attachments
                    return null;
                }

                FileCollection attachments = attachmentsFolder.Files;
                clientContext.Load(attachments);
                clientContext.ExecuteQuery();
                return attachments;
            }
        }

        #endregion Helpers

        #region Structure
        private ListCreationInformation BuildListCreationInformation(ClientContext context, Type entityType)
        {
            ListCreationInformation listCreationInfo = new ListCreationInformation
            {
                Title = EntityHelper.GetInternalNameFromEntityType(entityType),
                TemplateType = EntityHelper.GetListTemplateType(entityType),
                Description = EntityHelper.GetDescriptionFromEntityType(entityType),
            };
            int? documentTemplateType = GetTableDocumentTemplateType(entityType);
            if (documentTemplateType.HasValue) listCreationInfo.DocumentTemplateType = documentTemplateType.Value;

            QuickLaunchOptions? quickLaunchOption = GetQuickLaunchOption(entityType);
            if (quickLaunchOption.HasValue) listCreationInfo.QuickLaunchOption = quickLaunchOption.Value;

            string url = GetTableUrl(entityType);
            if (url != null) listCreationInfo.Url = url;

            return listCreationInfo;
        }

        private void SetRating<T>(List list)
        {
            var ratingAttribute = typeof(T).GetCustomAttribute<RatingAttribute>();
            if (ratingAttribute != null)
            {
                list.SetRating((OfficeDevPnP.Core.VotingExperience)ratingAttribute.VotingExperience);
            }
        }

        private void SetVersioning<T>(List list)
        {
            var versioningAttribute = typeof(T).GetCustomAttribute<VersioningAttribute>();
            if (versioningAttribute != null)
            {
                list.UpdateListVersioning(versioningAttribute.EnableVersioning, versioningAttribute.EnableMinorVersioning);
            }
        }

        public void CreateTable<T>()
        {
            Type entityType = typeof(T);
            string listName = EntityHelper.GetInternalNameFromEntityType(entityType);

            using (var clientContext = GetClientContext())
            {
                try
                {
                    ValidateBeforeListCreation(clientContext, listName);
                    Dictionary<string, Guid> lookupTableIds = GetLookupTableIds(clientContext, entityType);

                    ListCreationInformation listCreationInfo = BuildListCreationInformation(clientContext, entityType);

                    var newList = clientContext.Web.Lists.Add(listCreationInfo);
                    SetRating<T>(newList);
                    SetVersioning<T>(newList);
                    AddFieldsToTable(clientContext, newList, entityType.GetProperties(), lookupTableIds);
                    foreach (var property in entityType.GetProperties().Where(q => q.GetCustomAttribute<IgnoreOnCreationAttribute>() != null && q.GetCustomAttribute<DisplayNameAttribute>() != null))
                    {
                        // internal fields with custom displayname
                        _awecsomeField.ChangeDisplaynameFromField(newList, property);
                    }
                    foreach (var property in entityType.GetProperties().Where(q => q.GetCustomAttribute<IgnoreOnCreationAttribute>() != null && q.GetCustomAttribute<ChangeTypeOnCreationAttribute>() != null))
                    {
                        // internal fields with custom type
                        _awecsomeField.ChangeTypeFromField(newList, property);
                    }

                    clientContext.ExecuteQuery();
                }
                catch (Exception ex)
                {
                    var outerException = new Exception("error creating list", ex);
                    outerException.Data.Add("List", listName);

                    _log.Error($"Failed creating list {listName}", ex);
                    throw outerException;
                }
            }
            _log.Debug($"List '{listName}' created.");
        }

        private void AddFieldsToTable(ClientContext context, List sharePointList, PropertyInfo[] properties, Dictionary<string, Guid> lookupTableIds)
        {
            foreach (var property in properties)
            {
                try
                {
                    var managedMetadataAttribute = property.GetCustomAttribute<ManagedMetadataAttribute>();

                    Field newField = _awecsomeField.AddFieldToList(sharePointList, property, lookupTableIds);
                    if (newField != null && managedMetadataAttribute != null)
                    {
                        if (_awecsomeTaxonomy == null) _awecsomeTaxonomy = new AweCsomeTaxonomy(_clientContext);

                        // TODO: Type & Group configurable by attribute
                        _awecsomeTaxonomy.GetTermSetIds(TaxonomyTypes.SiteCollection, managedMetadataAttribute.TermSetName, null, managedMetadataAttribute.CreateIfMissing, out Guid termStoreId, out Guid termSetId);

                        context.ExecuteQuery();
                        Microsoft.SharePoint.Client.Taxonomy.TaxonomyField taxonomyField = context.CastTo<Microsoft.SharePoint.Client.Taxonomy.TaxonomyField>(newField);
                        taxonomyField.SspId = termStoreId;
                        taxonomyField.AllowMultipleValues = _awecsomeField.IsMulti(property.PropertyType);
                        taxonomyField.TermSetId = termSetId;
                        taxonomyField.TargetTemplate = string.Empty;
                        taxonomyField.AnchorId = Guid.Empty;
                        taxonomyField.Update();
                        context.ExecuteQuery();
                    }
                    else
                    {
                        context.ExecuteQuery();
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to create field '{property.Name}'", ex);
                    throw;
                }

            }
            context.ExecuteQuery();
            // TODO: Very Loooong tables: Split executeQuery
        }


        public string[] GetAvailableChoicesFromField<T>(string propertyName)
        {
            string listTitle = EntityHelper.GetDisplayNameFromEntitiyType(typeof(T));
            List sharePointList = _clientContext.Web.Lists.GetByTitle(listTitle);
            _clientContext.Load(sharePointList);
            _clientContext.ExecuteQuery();

            var property = typeof(T).GetProperty(propertyName);

            FieldChoice choiceField = _clientContext.CastTo<FieldChoice>(_awecsomeField.GetFieldDefinition(sharePointList, property));
            _clientContext.Load(choiceField, q => q.Choices);
            _clientContext.ExecuteQuery();

            return choiceField.Choices;
        }

        public void DeleteTable<T>()
        {
            DeleteTable(typeof(T), true);
        }

        private void DeleteTable(Type entityType, bool throwErrorIfMissing)
        {
            try
            {
                string listName = EntityHelper.GetInternalNameFromEntityType(entityType);
                using (var clientContext = GetClientContext())
                {
                    Web web = clientContext.Web;
                    ListCollection listCollection = web.Lists;
                    clientContext.Load(listCollection);
                    clientContext.ExecuteQuery();
                    List list = listCollection.FirstOrDefault(q => q.Title == listName);
                    if (list == null)
                    {
                        if (throwErrorIfMissing) throw new ListNotFoundException();
                        return;
                    }
                    list.DeleteObject();
                    clientContext.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Cannot delete table from entity of type '{entityType.Name}'", ex);
                throw;
            }
        }

        public void DeleteTableIfExisting<T>()
        {
            DeleteTable(typeof(T), false);
        }

        #endregion Structure

        #region Insert
        public int InsertItem<T>(T entity)
        {
            Type entityType = typeof(T);
            try
            {
                string listName = EntityHelper.GetInternalNameFromEntityType(entityType);
                using (var clientContext = GetClientContext())
                {
                    Web web = clientContext.Web;
                    ListCollection listCollection = web.Lists;
                    clientContext.Load(listCollection);
                    clientContext.ExecuteQuery();
                    List list = listCollection.FirstOrDefault(q => q.Title == listName);
                    if (list == null) throw new ListNotFoundException();

                    ListItem newItem = list.AddItem(new ListItemCreationInformation());
                    AssignPropertiesToListItem(entity, newItem);

                    newItem.Update();
                    clientContext.ExecuteQuery();
                    return newItem.Id;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Cannot insert data from entity of type '{entityType.Name}'", ex);
                throw;
            }
        }
        #endregion Insert

        #region Select

        private string WrapCamlQuery(string innerConditions)
        {
            return $"<View><Query><Where>{innerConditions}</Where></Query></View>";
        }

        private string CreateMultiCaml<T>(Dictionary<string, object> conditions, string conditionTypeName)
        {
            Type entityType = typeof(T);
            int conditionCount = 0;
            string conditionCaml = string.Empty;
            foreach (var condition in conditions)
            {
                conditionCount++;

                if (conditions.Count > 1 && conditionCount != conditions.Count)
                {
                    conditionCaml = $"<{conditionTypeName}>" + conditionCaml;
                }
                string singleConditionCaml;
                PropertyInfo fieldProperty = entityType.GetProperty(condition.Key);
                singleConditionCaml = EntityHelper.PropertyIsLookup(fieldProperty) ? CreateLookupCaml(condition.Key, (int)condition.Value, false) : CreateFieldEqCaml(fieldProperty, condition.Value, false);
                conditionCaml += "\n" + singleConditionCaml + "\n";
                if (conditionCount > 1)
                {
                    conditionCaml = conditionCaml + $"</{conditionTypeName}>";
                }
            }

            return WrapCamlQuery(conditionCaml);
        }

        private string CreateLookupCaml(string fieldname, int fieldvalue, bool wrapCamlQuery = true)
        {
            // TODO: Internal name
            string query = $"<Eq><FieldRef Name='{fieldname}' LookupId='TRUE' /><Value Type='Lookup'>{fieldvalue}</Value></Eq>";
            return wrapCamlQuery ? WrapCamlQuery(query) : query;
        }

        private string CreateFieldEqCaml(PropertyInfo property, object fieldvalue, bool wrapCamlQuery = true)
        {
            string fieldname = EntityHelper.GetInternalNameFromProperty(property);
            string fieldTypeName = EntityHelper.GetFieldType(property);
            string query = $"<Eq><FieldRef Name='{fieldname}' /><Value Type='{fieldTypeName}'>{fieldvalue}</Value></Eq>";
            return wrapCamlQuery ? WrapCamlQuery(query) : query;
        }

        public List<T> SelectItemsByFieldValue<T>(string fieldname, object value) where T : new()
        {
            Type entityType = typeof(T);
            PropertyInfo fieldProperty = entityType.GetProperty(fieldname);

            if (EntityHelper.PropertyIsLookup(fieldProperty)) return SelectItems<T>(new CamlQuery { ViewXml = CreateLookupCaml(fieldname, (int)value) });
            return SelectItems<T>(new CamlQuery { ViewXml = CreateFieldEqCaml(fieldProperty, value) });
        }

        private void StoreFromListItem<T>(T entity, ListItem item)
        {
            Type entityType = typeof(T);
            foreach (var property in entityType.GetProperties())
            {
                string fieldname = null;
                object sourceValue = null;
                Type sourceType = null;
                Type targetType = null;
                try
                {
                    if (!property.CanWrite) continue;
                    var ignoreOnSelectAttribute = property.GetCustomAttribute<IgnoreOnSelectAttribute>();
                    if (ignoreOnSelectAttribute != null && ignoreOnSelectAttribute.IgnoreOnSelect) continue;
                    fieldname = EntityHelper.GetInternalNameFromProperty(property);
                    if (item.FieldValues.ContainsKey(fieldname) && item.FieldValues[fieldname] != null)
                    {
                        sourceValue = item.FieldValues[fieldname];
                        targetType = property.PropertyType;
                        sourceType = sourceValue.GetType();

                        object propertyValue = EntityHelper.GetPropertyFromItemValue(property, item.FieldValues[fieldname]);
                        if (property.PropertyType.IsAssignableFrom(propertyValue.GetType()))
                        {
                            property.SetValue(entity, propertyValue);
                        }
                        else
                        {
                            property.SetValue(entity, Convert.ChangeType(propertyValue, property.PropertyType));
                        }
                    }
                    else if (fieldname == "Id")
                    {
                        property.SetValue(entity, item.Id);
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Could not store data from field '{fieldname}' ";
                    _log.Error(errorMessage, ex);
                    var exception = new Exception(errorMessage, ex);
                    exception.Data.Add("Field", fieldname);
                    exception.Data.Add("SourceValue", sourceValue);
                    exception.Data.Add("SourceType", sourceType);
                    exception.Data.Add("TargetType", targetType);
                }
            }
        }

        public List<T> SelectAllItems<T>() where T : new()
        {
            return SelectItems<T>(CamlQuery.CreateAllItemsQuery());
        }

        ListItem GetListItemById(string listname, int id)
        {
            using (var clientContext = GetClientContext())
            {
                Web web = clientContext.Web;
                ListCollection listCollection = web.Lists;
                clientContext.Load(listCollection);
                clientContext.ExecuteQuery();
                List list = listCollection.FirstOrDefault(q => q.Title == listname);
                if (list == null) throw new ListNotFoundException();
                ListItem item = list.GetItemById(id);
                clientContext.Load(item);
                clientContext.ExecuteQuery();
                return item;
            }
        }

        public T SelectItemById<T>(int id) where T : new()
        {
            Type entityType = typeof(T);
            var entity = new T();

            try
            {
                string listname = EntityHelper.GetInternalNameFromEntityType(entityType);
                var item = GetListItemById(listname, id);
                StoreFromListItem(entity, item);
                return entity;
            }
            catch (Exception ex)
            {
                _log.Error($"Cannot select item by id for '{entityType.Name}' with id '{id}'", ex);
                throw;
            }
        }

        private List<T> SelectItems<T>(CamlQuery query) where T : new()
        {
            Type entityType = typeof(T);
            var entities = new List<T>();

            try
            {
                string listName = EntityHelper.GetInternalNameFromEntityType(entityType);
                using (var clientContext = GetClientContext())
                {
                    Web web = clientContext.Web;
                    ListCollection listCollection = web.Lists;
                    clientContext.Load(listCollection);
                    clientContext.ExecuteQuery();
                    List list = listCollection.FirstOrDefault(q => q.Title == listName);
                    if (list == null) throw new ListNotFoundException();
                    ListItemCollection items = list.GetItems(query);
                    clientContext.Load(items);
                    clientContext.ExecuteQuery();
                    foreach (var item in items)
                    {
                        var entity = new T();
                        StoreFromListItem(entity, item);
                        entities.Add(entity);
                    }
                }
                return entities;
            }
            catch (Exception ex)
            {
                _log.Error($"Cannot select items from table of entity with type '{entityType.Name}", ex);
                throw;
            }
        }


        public List<T> SelectItemsByQuery<T>(string query) where T : new()
        {
            return SelectItems<T>(new CamlQuery() { ViewXml = query });
        }

        public List<T> SelectItemsByMultipleFieldValues<T>(Dictionary<string, object> conditions, bool isAndCondition = true) where T : new()
        {
            return SelectItems<T>(new CamlQuery { ViewXml = CreateMultiCaml<T>(conditions, isAndCondition ? "And" : "Or") });
        }

        #endregion Select

        #region Update

        public void UpdateItem<T>(T entity)
        {
            Type entityType = typeof(T);
            try
            {
                PropertyInfo idProperty = entityType.GetProperty(AweCsomeField.SuffixId);
                if (idProperty == null) throw new FieldMissingException("Field 'Id' is required for Update-Operations on Lists", AweCsomeField.SuffixId);
                int? idValue = idProperty.GetValue(entity) as int?;
                if (!idValue.HasValue) throw new FieldMissingException("Field 'Id' is has no value. Update failed", AweCsomeField.SuffixId);
                string listName = EntityHelper.GetInternalNameFromEntityType(entityType);
                using (var clientContext = GetClientContext())
                {
                    Web web = clientContext.Web;
                    ListCollection listCollection = web.Lists;
                    clientContext.Load(listCollection);
                    clientContext.ExecuteQuery();
                    List list = listCollection.FirstOrDefault(q => q.Title == listName);
                    if (list == null) throw new ListNotFoundException();
                    ListItem existingItem = list.GetItemById(idValue.Value);
                    foreach (var property in entityType.GetProperties())
                    {
                        if (!property.CanRead) continue;
                        var ignoreOnUpdateAttribute = property.GetCustomAttribute<IgnoreOnUpdateAttribute>();
                        if (ignoreOnUpdateAttribute != null && ignoreOnUpdateAttribute.IgnoreOnUpdate) continue;
                        existingItem[EntityHelper.GetInternalNameFromProperty(property)] = EntityHelper.GetItemValueFromProperty(property, entity);
                    }
                    existingItem.Update();
                    clientContext.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Cannot update data from entity of type '{entityType.Name}'", ex);
                throw;
            }
        }

        private void UpdateLikes(ListItem item, List<FieldUserValue> likeArray)
        {
            using (var clientContext = GetClientContext())
            {
                item["LikedBy"] = likeArray.ToArray();
                item["LikesCount"] = likeArray.Count;
                item.Update();
                clientContext.ExecuteQuery();
            }
        }

        public void Like<T>(int id, int userId)
        {
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));

            ListItem item = GetListItemById(listname, id);
            var likeArray = ((FieldUserValue[])item.FieldValues.First(fn => fn.Key == "LikedBy").Value)?.ToList() ?? new List<FieldUserValue>();
            var userLike = likeArray.FirstOrDefault(q => q.LookupId == userId);

            if (userLike == null)
            {
                likeArray.Add(new FieldUserValue { LookupId = userId });
                UpdateLikes(item, likeArray);
            }
        }

        public void Unlike<T>(int id, int userId)
        {
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));

            ListItem item = GetListItemById(listname, id);
            var likeArray = ((FieldUserValue[])item.FieldValues.First(fn => fn.Key == "LikedBy").Value).ToList();
            var userLike = likeArray.FirstOrDefault(q => q.LookupId == userId);

            if (userLike != null)
            {
                likeArray.Remove(userLike);
                UpdateLikes(item, likeArray);
            }
        }

        #endregion Update

        #region Delete

        public void DeleteItemById<T>(int id)
        {
            Type entityType = typeof(T);
            try
            {
                string listName = EntityHelper.GetInternalNameFromEntityType(entityType);
                using (var clientContext = GetClientContext())
                {
                    Web web = clientContext.Web;
                    ListCollection listCollection = web.Lists;
                    clientContext.Load(listCollection);
                    clientContext.ExecuteQuery();
                    List list = listCollection.FirstOrDefault(q => q.Title == listName);
                    if (list == null) throw new ListNotFoundException();
                    ListItem item = list.GetItemById(id);
                    item.DeleteObject();
                    clientContext.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Cannot delete item from table of entity of type '{entityType.Name}' with id '{id}'", ex);
                throw;
            }
        }

        #endregion Delete

        #region Files
        public List<string> SelectFileNamesFromItem<T>(int id)
        {
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));
            FileCollection attachments = GetAttachments(listname, id);
            return attachments.Select(q => q.Name).ToList();
        }

        public Dictionary<string, Stream> SelectFilesFromItem<T>(int id)
        {
            long totalSize = 0;
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));
            FileCollection attachments = GetAttachments(listname, id);

            var attachmentStreams = new Dictionary<string, Stream>();
            using (var clientContext = GetClientContext())
            {
                foreach (var attachment in attachments)
                {
                    MemoryStream targetStream = new MemoryStream();
                    var stream = attachment.OpenBinaryStream();
                    clientContext.ExecuteQuery();
                    stream.Value.CopyTo(targetStream);
                    attachmentStreams.Add(attachment.Name, targetStream);
                    totalSize += targetStream.Length;
                }
            }

            _log.DebugFormat($"Retrieved '{attachments.Count}' attachments from {listname}({id}). Size:{totalSize} Bytes");
            return attachmentStreams;
        }

        public void AttachFileToItem<T>(int id, string filename, Stream filestream)
        {
            long fileSize = filestream.Length;
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));
            using (ClientContext context = GetClientContext())
            {
                Web web = context.Web;
                List currentList = web.GetListByTitle(listname);
                ListItem item = currentList.GetItemById(id);
                var attachmentInfo = new AttachmentCreationInformation
                {
                    FileName = filename,
                    ContentStream = filestream
                };
                Attachment attachment = item.AttachmentFiles.Add(attachmentInfo);

                context.Load(attachment);
                context.ExecuteQuery();
                _log.DebugFormat($"Uploaded '{filename}' to {listname}({id}). Size:{fileSize} Bytes");
            }
        }

        public void DeleteFileFromItem<T>(int id, string filename)
        {
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));

            using (ClientContext context = GetClientContext())
            {
                Web web = context.Web;
                List currentList = web.GetListByTitle(listname);
                ListItem item = currentList.GetItemById(id);
                var allFiles = item.AttachmentFiles;
                context.Load(allFiles);
                context.ExecuteQuery();
                var oldFile = allFiles.FirstOrDefault(af => af.FileName == filename);
                if (oldFile == null) throw new FileNotFoundException($"File '{filename}' not found on {listname}/{id}");
                oldFile.DeleteObject();
                context.ExecuteQuery();
                _log.DebugFormat($"File '{filename}' deleted from {listname}/{id}");
            }
        }

        public string AttachFileToLibrary<T>(string foldername, string filename, Stream fileStream, T entity)
        {
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));
            var newFile = new FileCreationInformation
            {
                ContentStream = fileStream,
                Url = filename
            };

            using (ClientContext context = GetClientContext())
            {
                Web web = context.Web;
                List documentLibrary = web.GetListByTitle(listname);
                var targetFolder = documentLibrary.RootFolder;
                if (foldername != null)
                {
                    targetFolder = web.GetFolderByServerRelativeUrl($"{listname}\\{foldername}");
                }
                context.Load(targetFolder);
                context.ExecuteQuery();

                File uploadFile = targetFolder.Files.Add(newFile);

                uploadFile.ListItemAllFields.Update();
                context.ExecuteQuery();
                AssignPropertiesToListItem(entity, uploadFile.ListItemAllFields);
                uploadFile.ListItemAllFields.Update();
                context.ExecuteQuery();

                string targetFilename = $"{targetFolder.ServerRelativeUrl}/{filename}";
                _log.DebugFormat($"File '{filename}' uploaded to {targetFilename}");
                return targetFilename;
            }
        }

        public List<AweCsomeLibraryFile> SelectFilesFromLibrary<T>(string foldername) where T : new()
        {
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));
            var allFiles = new List<AweCsomeLibraryFile>();
            using (ClientContext context = GetClientContext())
            {
                Web web = context.Web;
                string folderUrl = $"{listname}\\{foldername}";
                var folder = web.GetFolderByServerRelativeUrl(folderUrl);

                if (folder == null) return null;
                try
                {
                    context.Load(folder, f => f.Exists);
                    context.ExecuteQuery();
                    if (!folder.Exists) return null;
                }
                catch
                {
                    return null; // There is no cleaner way to do this on CSOM
                }
                context.Load(folder.Files);
                context.Load(folder.Files, f => f.Include(q => q.ListItemAllFields));
                context.ExecuteQuery();
                if (folder.Files == null) return null;
                foreach (var file in folder.Files)
                {
                    var fileStream = file.OpenBinaryStream();
                    context.ExecuteQuery();
                    MemoryStream stream = new MemoryStream();
                    fileStream.Value.CopyTo(stream);
                    stream.Position = 0;
                    var entity = new T();

                    StoreFromListItem(entity, file.ListItemAllFields);
                    allFiles.Add(new AweCsomeLibraryFile
                    {
                        Filename = file.Name,

                        Stream = stream,
                        entity = entity
                    });
                }
                return allFiles;
            }
        }

        public AweCsomeLibraryFile SelectFileFromLibrary<T>(string foldername, string filename) where T : new()
        {
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));
            var allFiles = new List<AweCsomeLibraryFile>();
            using (ClientContext context = GetClientContext())
            {
                var folder = GetFolderFromDocumentLibrary<T>(context, foldername);
                context.Load(folder.Files);
                context.Load(folder.Files, f => f.Include(q => q.ListItemAllFields));
                context.ExecuteQuery();
                var file = folder.Files?.FirstOrDefault(q => q.Name == filename);
                if (file == null) return null;

                var fileStream = file.OpenBinaryStream();
                context.ExecuteQuery();
                MemoryStream stream = new MemoryStream();
                fileStream.Value.CopyTo(stream);
                stream.Position = 0;
                var entity = new T();

                StoreFromListItem(entity, file.ListItemAllFields);
                return new AweCsomeLibraryFile
                {
                    Filename = file.Name,

                    Stream = stream,
                    entity = entity
                };
            }
        }

        public string AddFolderToLibrary<T>(string folder)
        {
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));

            using (ClientContext context = GetClientContext())
            {
                Web web = context.Web;
                List documentLibrary = web.GetListByTitle(listname);
                var targetFolder = documentLibrary.RootFolder;
                context.Load(targetFolder);
                context.ExecuteQuery();
                string[] folderParts = folder.Split('/');
                foreach (string part in folderParts)
                {
                    targetFolder = targetFolder.EnsureFolder(part);
                }
                context.ExecuteQuery();
                return targetFolder.ServerRelativeUrl;
            }
        }

        public List<string> SelectFileNamesFromLibrary<T>(string foldername)
        {

            var allFiles = new List<AweCsomeLibraryFile>();
            using (ClientContext context = GetClientContext())
            {
                var folder = GetFolderFromDocumentLibrary<T>(context, foldername);
                context.Load(folder.Files);
                context.ExecuteQuery();
                if (folder.Files == null) return null;
                return folder.Files.Select(q => q.Name).ToList();
            }
        }

        public void DeleteFilesFromDocumentLibrary<T>(string path, List<string> filenames)
        {
            using (var context = GetClientContext())
            {
                var folder = GetFolderFromDocumentLibrary<T>(context, path);
                var existingFiles = folder.Files;
                context.Load(existingFiles);
                context.ExecuteQuery();
                foreach (string filename in filenames)
                {
                    existingFiles.First(q => q.Name == filename).DeleteObject();
                }
                context.ExecuteQuery();
            }
        }

        public void DeleteFolderFromDocumentLibrary<T>(string path, string foldername)
        {
            using (var context = GetClientContext())
            {
                var folder = GetFolderFromDocumentLibrary<T>(context, path);
                folder.DeleteObject();
                context.ExecuteQuery();
            }
        }

        #endregion Files

        #region Counts
        private int CountItems<T>(CamlQuery query)
        {
            Type entityType = typeof(T);
            try
            {
                string listName = EntityHelper.GetInternalNameFromEntityType(entityType);
                using (var clientContext = GetClientContext())
                {
                    Web web = clientContext.Web;
                    ListCollection listCollection = web.Lists;
                    clientContext.Load(listCollection);
                    clientContext.ExecuteQuery();
                    List list = listCollection.FirstOrDefault(q => q.Title == listName);
                    if (list == null) throw new ListNotFoundException();
                    ListItemCollection items = list.GetItems(query);
                    clientContext.Load(items, q => q.Include(l => l.Id));
                    clientContext.ExecuteQuery();
                    return items.Count;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Cannot select items from table of entity with type '{entityType.Name}", ex);
                throw;
            }
        }

        public int CountItems<T>()
        {
            return CountItems<T>(CamlQuery.CreateAllItemsQuery());
        }

        public int CountItemsByFieldValue<T>(string fieldname, object value)
        {
            Type entityType = typeof(T);
            PropertyInfo fieldProperty = entityType.GetProperty(fieldname);

            if (EntityHelper.PropertyIsLookup(fieldProperty)) return CountItems<T>(new CamlQuery { ViewXml = CreateLookupCaml(fieldname, (int)value) });
            return CountItems<T>(new CamlQuery { ViewXml = CreateFieldEqCaml(fieldProperty, value) });
        }

        public int CountItemsByMultipleFieldValues<T>(Dictionary<string, object> conditions, bool isAndCondition=true)
        {
            return CountItems<T>(new CamlQuery { ViewXml = CreateMultiCaml<T>(conditions, isAndCondition ? "And" : "Or") });
        }

        public int CountItemsByQuery<T>(string query)
        {
            return CountItems<T>(new CamlQuery { ViewXml = query });
        }


        #endregion Counts
    }
}
