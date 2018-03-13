using AweCsomeO365.Attributes.FieldAttributes;
using AweCsomeO365.Attributes.IgnoreAttributes;
using AweCsomeO365.Attributes.TableAttributes;
using AweCsomeO365.Exceptions;
using log4net;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365
{
    public class AweCsomeTable : IAweCsomeTable
    {
        private ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IAweCsomeField _awecsomeField = new AweCsomeField();
        private ClientContext _clientContext;

        public ClientContext ClientContext { set { _clientContext = value; } }

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
            var descriptionAttribute = entityType.GetCustomAttribute<Attributes.TableAttributes.UrlAttribute>();
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
                    AddFieldsToTable(clientContext, newList, entityType.GetProperties(), lookupTableIds);
                    clientContext.ExecuteQuery();
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed creating list {listName}", ex);
                    throw;
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
                    _awecsomeField.AddFieldToList(sharePointList, property, lookupTableIds);
                    context.ExecuteQuery();
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
                        if (!property.CanRead) continue;
                        if (property.GetCustomAttribute<IgnoreOnInsertAttribute>() != null) continue;
                        newItem[EntityHelper.GetInternalNameFromProperty(property)] = EntityHelper.GetPropertyValueForItem(property, entity);
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

        public List<T> SelectAllItems<T>()
        {
            throw new NotImplementedException();
        }

        private void StoreFromListItem<T>(T entity, ListItem item)
        {
            Type entityType = typeof(T);
            foreach (var property in entityType.GetProperties())
            {
                string fieldname=null;
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

                        object propertyValue = EntityHelper.GetItemValueForProperty(property, item.FieldValues[fieldname]);
                        property.SetValue(entity, Convert.ChangeType(propertyValue, property.PropertyType));
                    }
                } catch (Exception ex)
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

        public T SelectItemById<T>(int id) where T:new()
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
                _log.Error($"Cannot delete item from table of entity of type '{entityType.Name}' with id '{id}'", ex);
                throw;
            }

        }

        public List<T> SelectItemsByLookupId<T>(string fieldName, int lookupId)
        {
            throw new NotImplementedException();
        }

        public List<T> SelectItemsByNumber<T>(string fieldName, int number)
        {
            throw new NotImplementedException();
        }

        public List<T> SelectItemsByQuery<T>(string query)
        {
            throw new NotImplementedException();
        }

        public List<T> SelectItemsByString<T>(string fieldName, string queryValue)
        {
            throw new NotImplementedException();
        }

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
                        existingItem[EntityHelper.GetInternalNameFromProperty(property)] = EntityHelper.GetPropertyValueForItem(property, entity);
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

       
    }
}
