using AweCsome.Attributes.FieldAttributes;
using AweCsome.Attributes.IgnoreAttributes;
using AweCsome.Attributes.TableAttributes;
using AweCsome.Entities;
using AweCsome.Exceptions;
using AweCsome.Interfaces;

using AweCsome.Interfaces;

using log4net;

using Microsoft.SharePoint.Client;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using E = AweCsome.Enumerations;
using File = Microsoft.SharePoint.Client.File;

namespace AweCsome
{
    public class AweCsomeTable : IAweCsomeTable
    {
        private ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IAweCsomeField _awecsomeField = new AweCsomeField();
        private IAweCsomeTaxonomy _awecsomeTaxonomy = null;
        private ClientContext _clientContext;
        private int? _maxRetriesOnServerError = null;

        public int MaxRetriesOnServerError
        {
            get
            {
                if (_maxRetriesOnServerError != null) return _maxRetriesOnServerError.Value;
                _maxRetriesOnServerError = 1;

                var configSetting = ConfigurationManager.AppSettings["AweCsome.MaxRetriesOnServerError"];
                if (int.TryParse(configSetting, out int configSettingValue))
                {
                    _maxRetriesOnServerError = configSettingValue;
                }
                return _maxRetriesOnServerError.Value;

            }
        }

        public AweCsomeTable(ClientContext clientContext)
        {
            _clientContext = clientContext;
        }

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
            if (entity == null)
            {
                _log.Error("Nothing to assign");
                return;
            }
            Type entityType = typeof(T);
            foreach (var property in entityType.GetProperties())
            {
                try
                {
                    if (!property.CanRead) continue;
                    var value = EntityHelper.GetItemValueFromProperty(property, entity);
                    var ignoreAttribute = property.GetCustomAttribute<IgnoreOnInsertAttribute>();
                    if (ignoreAttribute != null && ignoreAttribute.IgnoreOnInsert)
                    {
                        if (!ignoreAttribute.OnlyIfEmpty) continue;
                        if (value == null) continue;
                    }

                    if (property.PropertyType == typeof(DateTime))
                    {
                        var year = ((DateTime)value).Year;
                        if (year < 1900 || year > 8900)
                        {
                            if (ignoreAttribute != null) continue;  // Empty Date
                            throw new ArgumentOutOfRangeException("SharePoint-Datetime must be within 1900 and 8900");
                        }
                    }
                    if (value != null) listItem[EntityHelper.GetInternalNameFromProperty(property)] = value;
                }
                catch (Exception ex)
                {
                    ex.Data.Add("Propertyname", property.Name);
                    //    ex.Data.Add("Listname", listItem);
                    throw (ex);
                }
            }
        }

        private E.QuickLaunchOptions? GetQuickLaunchOption(Type entityType)
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
                if (listname == entityType.Name) continue; // Self-Reference
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
#if ONPREM
                context.Load(folder, f => f.Name);
                context.ExecuteQuery();
                if (string.IsNullOrEmpty(folder.Name)) return null;
#else
                context.Load(folder, f => f.Exists);
                context.ExecuteQuery();
                if (!folder.Exists) return null;
#endif
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
                clientContext.Load(web, w => w.Url);
                clientContext.ExecuteQuery();
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

        private List GetList<T>(ClientContext clientContext)
        {
            Type entityType = typeof(T);
            string listName = EntityHelper.GetInternalNameFromEntityType(entityType);

            Web web = clientContext.Web;
            ListCollection listCollection = web.Lists;
            clientContext.Load(listCollection);
            clientContext.ExecuteQuery();
            List list = listCollection.FirstOrDefault(q => q.Title == listName);
            return list;
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

            E.QuickLaunchOptions? quickLaunchOption = GetQuickLaunchOption(entityType);
            if (quickLaunchOption.HasValue) listCreationInfo.QuickLaunchOption = (QuickLaunchOptions)quickLaunchOption.Value;

            //string url = GetTableUrl(entityType);
            //if (url != null) listCreationInfo.Url = url;

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

        public void UpdateTableStructure<T>()
        {
            Type entityType = typeof(T);
            string listName = EntityHelper.GetInternalNameFromEntityType(entityType);
            int columnsAddedCount = 0;
            int columnsModifiedCount = 0;
            int columnsRemovedCount = 0;

            int retries = MaxRetriesOnServerError;

            while (retries > 0)
            {
                try
                {
                    using (var clientContext = GetClientContext())
                    {
                        try
                        {
                            var existingList = GetList<T>(clientContext);
                            if (existingList == null) throw new ListNotFoundException();
                            Dictionary<string, Guid> lookupTableIds = GetLookupTableIds(clientContext, entityType);

                            var fields = existingList.Fields;
                            clientContext.Load(fields);
                            clientContext.ExecuteQuery();
                            List<string> fieldNames = new List<string>();
                            foreach (var field in fields.ToList())
                            {
                                if (!field.CanBeDeleted || field.Hidden || field.FieldTypeKind == FieldType.Invalid) continue;
                                fieldNames.Add(field.InternalName);
                                PropertyInfo fieldProperty = entityType.PropertyFromField(field.InternalName);
                                if (fieldProperty == null)
                                {
                                    field.DeleteObject();
                                    columnsRemovedCount++;
                                    _log.Debug($"Deleted field '{field.InternalName}'");
                                }
                                else
                                {
                                    var newFieldType = EntityHelper.GetFieldType(fieldProperty);
                                    if (newFieldType != field.FieldTypeKind.ToString())
                                    {
                                        _awecsomeField.ChangeTypeFromField(existingList, fieldProperty);
                                        columnsModifiedCount++;
                                        _log.Debug($"Modified field '{field.InternalName}' from {field.TypeAsString} to {newFieldType}");
                                    }
                                }
                            }

                            foreach (var property in entityType.GetProperties())
                            {
                                string internalName = EntityHelper.GetInternalNameFromProperty(property);
                                if (fieldNames.Contains(internalName)) continue;
                                var newField = _awecsomeField.AddFieldToList(existingList, property, lookupTableIds);
                                if (newField == null) continue;
                                columnsAddedCount++;
                                _log.Debug($"Added field '{internalName}'");
                            }

                            clientContext.ExecuteQuery();
                            retries = 0;

                            _log.Info($"Changed List '{listName}': Added {columnsAddedCount} fields, modified {columnsModifiedCount} fields and removed {columnsRemovedCount} fields");
                        }
                        catch (Exception ex)
                        {
                            var outerException = new Exception("error updating list", ex);
                            outerException.Data.Add("List", listName);

                            _log.Error($"Failed updating list {listName}", ex);
                            throw outerException;
                        }

                        _log.Debug($"List '{listName}' updated. {columnsAddedCount} columns have been added, {columnsModifiedCount} have been modified, {columnsRemovedCount} columns have been removed");
                    }
                }
                catch (Exception ex)
                {
                    retries--;
                    if (ex.Message.Contains("(500)") && retries > 0)
                    {
                        _log.Warn($"Internal Server Error. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                        System.Threading.Thread.Sleep(1000);
                    }
                    else throw;
                }
            }
        }

        public Guid CreateTable<T>()
        {
            Type entityType = typeof(T);
            string listName = EntityHelper.GetInternalNameFromEntityType(entityType);

            int retries = MaxRetriesOnServerError;
            while (retries > 0)
            {
                try
                {
                    using (var clientContext = GetClientContext())
                    {
                        try
                        {
                            ValidateBeforeListCreation(clientContext, listName);
                            Dictionary<string, Guid> lookupTableIds = GetLookupTableIds(clientContext, entityType);
                            ListCreationInformation listCreationInfo = BuildListCreationInformation(clientContext, entityType);

                            var newList = clientContext.Web.Lists.Add(listCreationInfo);

                            if (lookupTableIds.ContainsKey(listName))
                            {
                                _clientContext.Load(newList);
                                _clientContext.ExecuteQuery();

                                lookupTableIds[listName] = newList.Id;
                            }
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
                            clientContext.Load(newList, nl => nl.Id);
                            clientContext.ExecuteQuery();
                            _log.Debug($"List '{listName}' created.");
                            retries = 0;
                            return newList.Id;
                        }
                        catch (Exception ex)
                        {
                            //var outerException = new Exception("error creating list", ex);
                            //outerException.Data.Add("List", listName);

                            //_log.Error($"Failed creating list {listName}", ex);
                            //outerException.Message = ex.Message;
                            //throw outerException;
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    retries--;
                    if (ex.Message.Contains("(500)") && retries > 0)
                    {
                        _log.Warn($"Internal Server Error. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        _log.Error($"Cannot create List '{listName}", ex);
                        throw;
                    }
                }
            }
            throw new Exception("loop failed");
        }

        private void AddFieldsToTable(ClientContext context, List sharePointList, PropertyInfo[] properties, Dictionary<string, Guid> lookupTableIds)
        {
            foreach (var property in properties)
            {
                int retries = MaxRetriesOnServerError;
                while (retries > 0)
                {
                    try
                    {
                        var managedMetadataAttribute = property.GetCustomAttribute<ManagedMetadataAttribute>();

                        Field newField = (Field)_awecsomeField.AddFieldToList(sharePointList, property, lookupTableIds);
                        if (newField != null && managedMetadataAttribute != null)
                        {
                            if (_awecsomeTaxonomy == null) _awecsomeTaxonomy = new AweCsomeTaxonomy(_clientContext);

                            // TODO: Type & Group configurable by attribute
                            _awecsomeTaxonomy.GetTermSetIds(E.TaxonomyTypes.SiteCollection, managedMetadataAttribute.TermSetName, null, managedMetadataAttribute.CreateIfMissing, out Guid termStoreId, out Guid termSetId);

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
                        retries = 0;
                    }
                    catch (Exception ex)
                    {
                        retries--;
                        if (ex.Message.Contains("(500)") && retries > 0)
                        {
                            _log.Warn($"Failed to create field '{property.Name}'. Internal Server Error. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                            System.Threading.Thread.Sleep(1000);
                        }
                        else
                        {
                            _log.Error($"Failed to create field '{property.Name}'", ex);
                            throw;
                        }
                    }
                }
            }
            context.ExecuteQuery();
            // TODO: Very Loooong tables: Split executeQuery
        }

        public string[] GetAvailableChoicesFromField<T>(string propertyName)
        {
            string listTitle = EntityHelper.GetDisplayNameFromEntityType(typeof(T));
            List sharePointList = _clientContext.Web.Lists.GetByTitle(listTitle);
            _clientContext.Load(sharePointList);
            _clientContext.ExecuteQuery();

            var property = typeof(T).GetProperty(propertyName);

            var field = (Field)_awecsomeField.GetFieldDefinition(sharePointList, property);
            FieldChoice choiceField = _clientContext.CastTo<FieldChoice>(field);
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
                _log.Debug($"List '{listName}' deleted ");
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
            int retries = MaxRetriesOnServerError;
            while (retries > 0)
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
                        if (list == null) throw new ListNotFoundException();

                        ListItem newItem = list.AddItem(new ListItemCreationInformation());
                        AssignPropertiesToListItem(entity, newItem);

                        newItem.Update();
                        clientContext.ExecuteQuery();
                        retries = 0;
                        return newItem.Id;
                    }
                }
                catch (Microsoft.SharePoint.Client.ServerException ex)
                {
                    _log.Warn($"ErrorCode: {ex.ServerErrorCode},Correlation: {ex.ServerErrorTraceCorrelationId}, Type: {ex.ServerErrorTypeName}, Value: {ex.ServerErrorValue}");
                    throw;
                }
                catch (Exception ex)
                {
                    retries--;
                    if (ex.Message.Contains("(500)") && retries > 0)
                    {
                        _log.Warn($"Internal Server Error. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        _log.Error($"Cannot insert data from entity of type '{entityType.Name}", ex);
                        throw;
                    }
                }
            }
            throw new Exception("too man retries");
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

        public List<T> SelectItemsByTitle<T>(string title) where T : new()
        {
            return SelectItemsByFieldValue<T>("Title", title);
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
                        targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                        sourceType = Nullable.GetUnderlyingType(sourceValue.GetType()) ?? sourceValue.GetType();

                        object propertyValue = EntityHelper.GetPropertyFromItemValue(property, item.FieldValues[fieldname]);
                        if (property.PropertyType.IsAssignableFrom(propertyValue.GetType()))
                        {
                            property.SetValue(entity, propertyValue);
                        }
                        else if (targetType == typeof(int) && sourceValue is FieldLookupValue)
                        {
                            property.SetValue(entity, ((FieldLookupValue)sourceValue).LookupId);
                        }
                        else
                        {
                            property.SetValue(entity, Convert.ChangeType(propertyValue, targetType));
                        }
                    }
                    else if (fieldname == "Id")
                    {
                        property.SetValue(entity, item.Id);
                    }
                }
                catch (Microsoft.SharePoint.Client.ServerException ex)
                {
                    _log.Warn($"ErrorCode: {ex.ServerErrorCode},Correlation: {ex.ServerErrorTraceCorrelationId}, Type: {ex.ServerErrorTypeName}, Value: {ex.ServerErrorValue}");
                    string errorMessage = $"Could not store data from field '{fieldname}' ";
                    _log.Error(errorMessage, ex);
                    var exception = new Exception(errorMessage, ex);
                    exception.Data.Add("Field", fieldname);
                    exception.Data.Add("SourceValue", sourceValue);
                    exception.Data.Add("SourceType", sourceType);
                    exception.Data.Add("TargetType", targetType);
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

        public bool Exists<T>()
        {
            int retries = MaxRetriesOnServerError;
            while (retries > 0)
            {
                try
                {
                    using (var clientContext = GetClientContext())
                    {
                        var web = clientContext.Web;
                        clientContext.Load(web);
                        clientContext.ExecuteQuery();
                        bool listExists = web.ListExists(EntityHelper.GetInternalNameFromEntityType(typeof(T)));
                        retries = 0;
                        return listExists;
                    }
                }
                catch (Exception ex)
                {
                    retries--;
                    if (ex.Message.Contains("(500)") && retries > 0)
                    {
                        _log.Warn($"Internal Server. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        throw new Exception($"Cannot check if list '{typeof(T).Name}' exists", ex);
                        throw;
                    }
                }
            }
            throw new Exception("too many retries");
        }

        private ListItem GetListItemById(string listname, int id)
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

            int retries = MaxRetriesOnServerError;
            while (retries > 0)
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
                    retries = 0;
                    return entities;
                }
                catch (Exception ex)
                {
                    retries--;
                    if (ex.Message.Contains("(500)") && retries > 0)
                    {
                        _log.Warn($"Internal Server Error. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        _log.Error($"Cannot select items from table of entity with type '{entityType.Name}", ex);
                        throw;
                    }
                }
            }
            throw new Exception("Too many retries");
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
            if (entity == null) throw new Exception("Entity is null");
            Type entityType = entity.GetType();
            int retries = MaxRetriesOnServerError;
            while (retries > 0)
            {
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
                            try
                            {
                                if (!property.CanRead) continue;
                                var value = EntityHelper.GetItemValueFromProperty(property, entity);
                                var ignoreOnUpdateAttribute = property.GetCustomAttribute<IgnoreOnUpdateAttribute>();
                                if (ignoreOnUpdateAttribute != null && ignoreOnUpdateAttribute.IgnoreOnUpdate)
                                {
                                    if (!ignoreOnUpdateAttribute.OnlyIfEmpty) continue;
                                    if (value == null) continue;
                                }
                                if (property.PropertyType == typeof(DateTime))
                                {
                                    var year = ((DateTime)value).Year;
                                    if (year < 1900 || year > 8900)
                                    {
                                        if (ignoreOnUpdateAttribute != null) continue;  // Empty Date
                                        throw new ArgumentOutOfRangeException("SharePoint-Datetime must be within 1900 and 8900");
                                    }
                                }

                                if (value is KeyValuePair<int, string> && ((KeyValuePair<int, string>)value).Key == 0) value = null; // Lookup/Person with no value
                                existingItem[EntityHelper.GetInternalNameFromProperty(property)] = value;
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Error in assigning value from field '{property?.Name}'", ex);
                            }
                        }
                        existingItem.Update();
                        clientContext.ExecuteQuery();
                        retries = 0;
                    }
                }
                catch (Exception ex)
                {
                    retries--;
                    if (ex.Message.Contains("(500)") && retries > 0)
                    {
                        _log.Warn($"Internal Server Error. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        throw new Exception($"Cannot update data from entity of type '{entityType?.Name}'", ex);
                        throw;
                    }
                }
            }
        }

        private void UpdateLikes(ListItem item, List<FieldUserValue> likeArray)
        {
            int retries = MaxRetriesOnServerError;
            while (retries > 0)
            {
                try
                {
                    using (var clientContext = GetClientContext())
                    {
                        item["LikedBy"] = likeArray.ToArray();
                        item["LikesCount"] = likeArray.Count;
                        item.Update();
                        clientContext.ExecuteQuery();
                        retries = 0;
                    }
                }
                catch (Exception ex)
                {
                    retries--;
                    if (ex.Message.Contains("(500)") && retries > 0)
                    {
                        _log.Warn($"Internal Server Error. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        throw new Exception($"Cannot update likes ", ex);
                        throw;
                    }
                }
            }
        }

        public bool IsLikedBy<T>(int id, int userId) where T : new()
        {
            var likes = GetLikes<T>(id);
            return likes.ContainsKey(userId);
        }

        public Dictionary<int, string> GetLikes<T>(int id) where T : new()
        {
            int retries = MaxRetriesOnServerError;
            while (retries > 0)
            {
                try
                {
                    string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));
                    ListItem item = GetListItemById(listname, id);
                    var likeArray = ((FieldUserValue[])item.FieldValues.First(fn => fn.Key == "LikedBy").Value)?.ToList() ?? new List<FieldUserValue>();
                    var likes = new Dictionary<int, string>();
                    foreach (var like in likeArray)
                    {
                        likes.Add(like.LookupId, like.LookupValue);
                    }
                    retries = 0;
                    return likes;
                }
                catch (Exception ex)
                {
                    retries--;
                    if (ex.Message.Contains("(500)") && retries > 0)
                    {
                        _log.Warn($"Internal Server Error. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        throw new Exception($"Cannot get likes", ex);
                    }
                }
            }
            throw new Exception("too many retries");
        }

        public T Like<T>(int id, int userId) where T : new()
        {
            int retries = MaxRetriesOnServerError;
            while (retries > 0)
            {
                try
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
                    var entity = new T();
                    StoreFromListItem(entity, item);
                    retries = 0;
                    return entity;
                }
                catch (Exception ex)
                {
                    retries--;
                    if (ex.Message.Contains("(500)") && retries > 0)
                    {
                        _log.Warn($"Internal Server Error. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        throw new Exception($"Cannot like", ex);
                        throw;
                    }
                }
            }
            throw new Exception("too many retries");
        }

        public T Unlike<T>(int id, int userId) where T : new()
        {
            int retries = MaxRetriesOnServerError;
            while (retries > 0)
            {
                try
                {
                    string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));

                    ListItem item = GetListItemById(listname, id);
                    var likeArray = ((FieldUserValue[])item.FieldValues.First(fn => fn.Key == "LikedBy").Value)?.ToList() ?? new List<FieldUserValue>();
                    var userLike = likeArray.FirstOrDefault(q => q.LookupId == userId);

                    if (userLike != null)
                    {
                        likeArray.Remove(userLike);
                        UpdateLikes(item, likeArray);
                    }
                    var entity = new T();
                    StoreFromListItem(entity, item);
                    retries = 0;
                    return entity;
                }
                catch (Exception ex)
                {
                    retries--;
                    if (ex.Message.Contains("(500)") && retries > 0)
                    {
                        _log.Warn($"Internal Server Error. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        throw new Exception($"Cannot like", ex);
                        throw;
                    }
                }
            }
            throw new Exception("too many retries");
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

        public void Empty<T>()
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
                    var items = list.GetItems(CamlQuery.CreateAllItemsQuery());
                    clientContext.Load(items);
                    clientContext.ExecuteQuery();
                    while (items.Count > 0)
                    {
                        items.First().DeleteObject();
                    }
                    clientContext.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Cannot emptytable of entity of type '{entityType.Name}' ", ex);
                throw;
            }
        }

        #endregion Delete

        #region Files

        public List<KeyValuePair<DateTime, string>> SelectFileNamesFromItem<T>(int id)
        {
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));
            FileCollection attachments = GetAttachments(listname, id);
            if (attachments == null) return new List<KeyValuePair<DateTime, string>>();

            return attachments.Select(q => new KeyValuePair<DateTime, string>(q.TimeCreated, q.Name)).ToList();
        }

        private AweCsomeFile CastFileToAweCsomeFile(File file, string folder, object entity)
        {
            using (var clientContext = GetClientContext())
            {
                clientContext.Load(file.Author);
                clientContext.Load(file.CheckedOutByUser);

                MemoryStream targetStream = new MemoryStream();
                var stream = file.OpenBinaryStream();
                clientContext.ExecuteQuery();
                stream.Value.CopyTo(targetStream);

                int authorId = file.Author.Id;
                int? checkedOutByUser = null;
                if (file.CheckOutType != CheckOutType.None) checkedOutByUser = file.CheckedOutByUser.Id;

                var virusStatus = file.ListItemAllFields["_VirusStatus"];
                _log.Debug($"Vstatus: {virusStatus}. Type: {virusStatus?.GetType()}");
                var status = AweCsomeFile.VirusStatusValues.Clean;
                if (virusStatus != null) status = (AweCsomeFile.VirusStatusValues)(int)virusStatus;

                return new AweCsomeFile
                {
                    Author = authorId,
                    CheckedOutBy = checkedOutByUser,
                    CheckInComment = file.CheckInComment,
                    CheckoutType = (AweCsomeFile.CheckoutTypes)Enum.Parse(typeof(AweCsomeFile.CheckoutTypes), file.CheckOutType.ToString()),
                    Created = file.TimeCreated,
                    Filename = file.Name,
                    Length = file.Length,
                    Modified = file.TimeLastModified,
                    VirusStatus = status,
                    Level = (AweCsomeFile.FileLevels)Enum.Parse(typeof(AweCsomeFile.FileLevels), file.Level.ToString()),
                    Version = $"{file.MajorVersion}.{file.MinorVersion}",
                    Stream = targetStream,
                    Entity = entity,
                    Folder = folder
                };
            }
        }

        public List<AweCsomeFile> SelectFilesFromItem<T>(int id, string filename = null)
        {
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));
            FileCollection attachments = GetAttachments(listname, id);

            var attachmentStreams = new List<AweCsomeFile>();

            //var attachmentStreams = new Dictionary<string, Stream>();
            using (var clientContext = GetClientContext())
            {
                if (attachments != null)
                {
                    foreach (var attachment in attachments)
                    {
                        if (filename != null && filename != attachment.Name) continue;
                        attachmentStreams.Add(CastFileToAweCsomeFile(attachment, null, null));
                    }
                }
            }

            long totalSize = attachmentStreams.Sum(q => q.Length);
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

        public List<AweCsomeFile> SelectFilesFromLibrary<T>(string foldername, bool retrieveContent = true) where T : new()
        {
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));
            var allFiles = new List<AweCsomeFile>();
            using (ClientContext context = GetClientContext())
            {
                Web web = context.Web;
                string folderUrl = $"{listname}\\{foldername}";
                var folder = web.GetFolderByServerRelativeUrl(folderUrl);

                if (folder == null) return null;
                try
                {
#if ONPREM
                    context.Load(folder, f => f.Name);
                    context.ExecuteQuery();
                    if (string.IsNullOrEmpty(folder.Name)) return null;
#else
                    context.Load(folder, f => f.Exists);
                    context.ExecuteQuery();
                    if (!folder.Exists) return null;
#endif
                }
                catch
                {
                    return null; // Sadly there is no cleaner way to do this on CSOM
                }
                context.Load(folder.Files);
                context.Load(folder.Files, f => f.Include(q => q.ListItemAllFields));
                context.ExecuteQuery();
                if (folder.Files == null) return null;
                foreach (var file in folder.Files)
                {
                    var entity = new T();
                    StoreFromListItem(entity, file.ListItemAllFields);
                    allFiles.Add(CastFileToAweCsomeFile(file, foldername, entity));
                }
                return allFiles;
            }
        }

        public AweCsomeFile SelectFileFromLibrary<T>(string foldername, string filename) where T : new()
        {
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));
            var allFiles = new List<AweCsomeFile>();
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
                return new AweCsomeFile
                {
                    Filename = file.Name,

                    Stream = stream,
                    Entity = entity
                };
            }
        }

        public string AddFolderToLibrary<T>(string folder)
        {
            string listname = EntityHelper.GetInternalNameFromEntityType(typeof(T));
            int retries = MaxRetriesOnServerError;
            while (retries > 0)
            {
                try
                {
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
                        retries = 0;
                        return targetFolder.ServerRelativeUrl;
                    }
                }
                catch (Exception ex)
                {
                    retries--;
                    if (ex.Message.Contains("(500)") && retries > 0)
                    {
                        _log.Warn($"Internal Server Error. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        _log.Error($"Cannot add folder '{folder}' to '{typeof(T).Name}", ex);
                        throw;
                    }
                }
            }
            throw new Exception("too many retries");
        }

        public List<string> SelectFileNamesFromLibrary<T>(string foldername)
        {
            var allFiles = new List<AweCsomeFile>();
            int retries = MaxRetriesOnServerError;
            while (retries > 0)
            {
                try
                {
                    using (ClientContext context = GetClientContext())
                    {
                        var folder = GetFolderFromDocumentLibrary<T>(context, foldername);
                        context.Load(folder.Files);
                        context.ExecuteQuery();
                        if (folder.Files == null) return null;
                        retries = 0;
                        return folder.Files.Select(q => q.Name).ToList();
                    }
                }
                catch (Exception ex)
                {
                    retries--;
                    if (ex.Message.Contains("(500)") && retries > 0)
                    {
                        _log.Warn($"Internal Server Error. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        _log.Error($"Cannot select filenames from folder {foldername}", ex);
                        throw;
                    }
                }
            }
            throw new Exception("too many retries");
        }

        public void DeleteFilesFromDocumentLibrary<T>(string path, List<string> filenames)
        {
            int retries = MaxRetriesOnServerError;
            while (retries > 0)
            {
                try
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
                        retries = 0;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    retries--;
                    if (ex.Message.Contains("(500)") && retries > 0)
                    {
                        _log.Warn($"Internal Server Error. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        _log.Error($"Cannot delete files from '{path}' of entity with type '{typeof(T).Name}", ex);
                        throw;
                    }
                }
            }
        }

        public void DeleteFolderFromDocumentLibrary<T>(string path, string foldername)
        {
            int retries = MaxRetriesOnServerError;
            while (retries > 0)
            {
                try
                {
                    using (var context = GetClientContext())
                    {
                        var folder = GetFolderFromDocumentLibrary<T>(context, path);
                        folder.DeleteObject();
                        context.ExecuteQuery();
                        retries = 0;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    retries--;
                    if (ex.Message.Contains("(500)") && retries > 0)
                    {
                        _log.Warn($"Internal Server Error. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        _log.Error($"Cannot delete from folder '{path}' of entity with type '{typeof(T).Name}", ex);
                        throw;
                    }
                }
            }
        }

        #endregion Files

        #region Counts

        private int CountItems<T>(CamlQuery query)
        {
            Type entityType = typeof(T);

            int retries = MaxRetriesOnServerError;
            while (retries > 0)
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
                        if (list == null) throw new ListNotFoundException();
                        ListItemCollection items = list.GetItems(query);
                        clientContext.Load(items, q => q.Include(l => l.Id));
                        clientContext.ExecuteQuery();
                        retries = 0;
                        return items.Count;
                    }
                }
                catch (Exception ex)
                {
                    retries--;
                    if (ex.Message.Contains("(500)") && retries > 0)
                    {
                        _log.Warn($"Internal Server Error. Will try again. ErrorCount: {MaxRetriesOnServerError - retries}");
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        _log.Error($"Cannot count items from table of entity with type '{entityType.Name}", ex);
                        throw;
                    }
                }
            }
            throw new Exception("too many retries");
        }

        public int CountItems<T>()
        {
            return CountItems<T>(CamlQuery.CreateAllItemsQuery());
        }

        public int CountItemsByFieldValue<T>(string fieldname, object value) where T : new()
        {
            Type entityType = typeof(T);
            PropertyInfo fieldProperty = entityType.GetProperty(fieldname);

            if (EntityHelper.PropertyIsLookup(fieldProperty)) return CountItems<T>(new CamlQuery { ViewXml = CreateLookupCaml(fieldname, (int)value) });
            return CountItems<T>(new CamlQuery { ViewXml = CreateFieldEqCaml(fieldProperty, value) });
        }

        public int CountItemsByMultipleFieldValues<T>(Dictionary<string, object> conditions, bool isAndCondition = true)
        {
            return CountItems<T>(new CamlQuery { ViewXml = CreateMultiCaml<T>(conditions, isAndCondition ? "And" : "Or") });
        }

        public int CountItemsByQuery<T>(string query)
        {
            return CountItems<T>(new CamlQuery { ViewXml = query });
        }

        #endregion Counts

        #region Changes

        public bool HasChangesSince<T>(DateTime compareDate) where T : new()
        {
            return ModifiedItemsSince<T>(compareDate).Count > 0;
        }

        public List<KeyValuePair<AweCsomeListUpdate, T>> ModifiedItemsSince<T>(DateTime compareDate) where T : new()
        {
            var modifiedItems = new List<KeyValuePair<AweCsomeListUpdate, T>>();
            using (var clientContext = GetClientContext())
            {
                List list = GetList<T>(clientContext);
                if (list == null) throw new ListNotFoundException();
                var changeQuery = new ChangeQuery(false, false);
                changeQuery.Item = true;
                changeQuery.Update = true;
                changeQuery.DeleteObject = true;
                changeQuery.Add = true;

                changeQuery.ChangeTokenStart = new ChangeToken();
                changeQuery.ChangeTokenStart.StringValue = string.Format("1;3;{0};{1};-1", list.Id.ToString(), compareDate.ToUniversalTime().Ticks.ToString());

                var changeCollection = list.GetChanges(changeQuery);
                clientContext.Load(changeCollection);
                clientContext.ExecuteQuery();
                var changeItemCollection = new List<ChangeItem>();

                foreach (var change in changeCollection)
                {
                    if (!(change is ChangeItem)) continue;
                    changeItemCollection.Add((ChangeItem)change);
                }

                foreach (var changeItem in changeItemCollection)
                {
                    var updateInfo = new AweCsomeListUpdate { ChangeDate = changeItem.Time, Id = changeItem.ItemId };
                    switch (changeItem.ChangeType)
                    {
                        case ChangeType.Add:
                            updateInfo.ChangeType = AweCsomeListUpdate.ChangeTypes.Add;
                            break;

                        case ChangeType.DeleteObject:
                            updateInfo.ChangeType = AweCsomeListUpdate.ChangeTypes.Delete;
                            break;

                        case ChangeType.Update:
                            updateInfo.ChangeType = AweCsomeListUpdate.ChangeTypes.Update;
                            break;
                    }
                    T itemContent = default(T);
                    bool hasBeenDeletedLaterOn = changeItemCollection.Any(q => q.ItemId == changeItem.ItemId && q.ChangeType == ChangeType.DeleteObject);
                    if (!hasBeenDeletedLaterOn)
                    {
                        itemContent = SelectItemById<T>(changeItem.ItemId);
                    }
                    modifiedItems.Add(new KeyValuePair<AweCsomeListUpdate, T>(updateInfo, itemContent));
                }
            }
            return modifiedItems;
        }

        #endregion Changes
    }
}