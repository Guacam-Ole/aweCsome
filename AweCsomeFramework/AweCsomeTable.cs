using AweCsome.Attributes.FieldAttributes;
using AweCsome.Attributes.IgnoreAttributes;
using AweCsome.Attributes.TableAttributes;
using AweCsome.Exceptions;
using log4net;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome
{
    public class AweCsomeTable : IAweCsomeTable
    {
        private ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IAweCsomeField _awecsomeField = new AweCsomeField();
        private IAweCsomeTaxonomy _awecsomeTaxonomy = null;
        private ClientContext _clientContext;

        public ClientContext ClientContext { set { _clientContext = value; } }

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
            if (ratingAttribute!=null)
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

                    Field newField=_awecsomeField.AddFieldToList(sharePointList, property, lookupTableIds);
                    if (newField!=null && managedMetadataAttribute!=null)
                    {
                        if (_awecsomeTaxonomy == null) _awecsomeTaxonomy = new AweCsomeTaxonomy { ClientContext = _clientContext };

                        _awecsomeTaxonomy.GetTaxonomyFieldInfo(managedMetadataAttribute.TermSetName, managedMetadataAttribute.CreateIfMissing, out Guid termStoreId, out Guid termSetId);

                        context.ExecuteQuery();
                        Microsoft.SharePoint.Client.Taxonomy.TaxonomyField taxonomyField = context.CastTo<Microsoft.SharePoint.Client.Taxonomy.TaxonomyField>(newField);
                        taxonomyField.SspId = termStoreId;
                        taxonomyField.AllowMultipleValues = _awecsomeField.IsMulti(property.PropertyType);
                        taxonomyField.TermSetId = termSetId;
                        taxonomyField.TargetTemplate = string.Empty;
                        taxonomyField.AnchorId = Guid.Empty;
                        taxonomyField.Update();
                        //context.ExecuteQuery();
                    } else
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
                    foreach (var property in entityType.GetProperties())
                    {
                        try
                        {
                            if (!property.CanRead) continue;
                            if (property.GetCustomAttribute<IgnoreOnInsertAttribute>() != null) continue;
                            newItem[EntityHelper.GetInternalNameFromProperty(property)] = EntityHelper.GetItemValueFromProperty(property, entity);
                        } catch (Exception ex)
                        {
                            ex.Data.Add("Propertyname", property.Name);
                            ex.Data.Add("Listname", listName);
                            throw (ex);
                        }
                    }
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

        private string CreateLookupCaml(string fieldname, int fieldvalue)
        {
            // TODO: Internal name
            return WrapCamlQuery($"<Eq><FieldRef Name='{fieldname}' LookupId='TRUE' /><Value Type='Lookup'>{fieldvalue}</Value></Eq>");
        }

        private string CreateFieldEqCaml(PropertyInfo property, object fieldvalue)
        {
            string fieldname = EntityHelper.GetInternalNameFromProperty(property);
            string fieldTypeName = EntityHelper.GetFieldType(property);
            return WrapCamlQuery($"<Eq><FieldRef Name='{fieldname}' /><Value Type='{fieldTypeName}'>{fieldvalue}</Value></Eq>");
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
                    if (property.GetCustomAttribute<IgnoreOnSelectAttribute>() != null) continue;
                    fieldname = EntityHelper.GetInternalNameFromProperty(property);
                    if (item.FieldValues.ContainsKey(fieldname) && item.FieldValues[fieldname] != null)
                    {
                        sourceValue = item.FieldValues[fieldname];
                        targetType = property.PropertyType;
                        sourceType = sourceValue.GetType();

                        object propertyValue = EntityHelper.GetPropertyFromItemValue(property, item.FieldValues[fieldname]);
                        property.SetValue(entity, Convert.ChangeType(propertyValue, property.PropertyType));
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

        public T SelectItemById<T>(int id) where T : new()
        {
            Type entityType = typeof(T);
            var entity = new T();

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
                    clientContext.Load(item);
                    clientContext.ExecuteQuery();
                    StoreFromListItem(entity, item);
                }
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
                        if (property.GetCustomAttribute<IgnoreOnUpdateAttribute>() != null) continue;
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
    }
}
