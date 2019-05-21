using AweCsome.Attributes.FieldAttributes;
using AweCsome.Buffer.Attributes;
using AweCsome.Interfaces;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AweCsome.Buffer
{
    public class LiteDbQueue : LiteDb
    {
        private static object _queueLock = new object();
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IAweCsomeTable _aweCsomeTable;

        public LiteDbQueue(IAweCsomeHelpers helpers, IAweCsomeTable aweCsomeTable, string databaseName) : base(helpers, databaseName, true)
        {
            _aweCsomeTable = aweCsomeTable;
        }

        public void AddCommand(Command command)
        {
            lock (_queueLock)
            {
                var commandCollection = GetCollection<Command>();
                var maxId = commandCollection.Max(q => q.Id).AsInt32;
                command.Id = maxId + 1;
                commandCollection.Insert(command);
            }
        }

        public List<Command> Read()
        {
            return GetCollection<Command>(null).FindAll().OrderBy(q => q.Created).ToList();
        }

        public void Update(Command command)
        {
            GetCollection<Command>(null).Update(command);
        }

        private MethodInfo GetMethod<T>(Expression<Action<T>> expr)
        {
            return ((MethodCallExpression)expr.Body)
                .Method
                .GetGenericMethodDefinition();
        }

        private object CallGenericMethod(object baseObject, MethodInfo method, Type baseType, string fullyQualifiedName, object[] parameters)
        {
            Type entityType = baseType.Assembly.GetType(fullyQualifiedName, false, true);
            MethodInfo genericMethod = method.MakeGenericMethod(entityType);
            var paams = genericMethod.GetParameters();
            try
            {
                var retVal = genericMethod.Invoke(baseObject, parameters);
                return retVal;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException.GetType() != typeof(Exception)) throw ex.InnerException;
                throw;
            }
        }

        private string GetListNameFromFullyQualifiedName(Type baseType, string fullyQualifiedName)
        {
            return baseType.Assembly.GetType(fullyQualifiedName, false, true).Name;
        }

        public void QueueCreateTable(Type baseType, Command command)
        {
            MethodInfo method = GetMethod<IAweCsomeTable>(q => q.CreateTable<object>());
            CallGenericMethod(_aweCsomeTable, method, baseType, command.FullyQualifiedName, null);
        }

        public void QueueDeleteTable(Type baseType, Command command)
        {
            MethodInfo method = GetMethod<IAweCsomeTable>(q => q.DeleteTable<object>());
            CallGenericMethod(_aweCsomeTable, method, baseType, command.FullyQualifiedName, null);
        }

        public void QueueEmpty(Type baseType, Command command)
        {
            MethodInfo method = GetMethod<IAweCsomeTable>(q => q.Empty<object>());
            CallGenericMethod(_aweCsomeTable, method, baseType, command.FullyQualifiedName, null);
        }

        public void QueueInsert(Type baseType, Command command)
        {
            object insertData = GetFromDbById(baseType, command.FullyQualifiedName, command.ItemId.Value);
            MethodInfo method = GetMethod<IAweCsomeTable>(q => q.InsertItem<object>(insertData));
            int newId = (int)CallGenericMethod(_aweCsomeTable, method, baseType, command.FullyQualifiedName, new object[] { insertData });
            UpdateId(baseType, command.FullyQualifiedName, command.ItemId.Value, newId);
        }

        private object GetFromDbById(Type baseType, string fullyQualifiedName, int id)
        {
            var db = new LiteDb(_helpers, _databaseName);
            MethodInfo method = GetMethod<LiteDb>(q => q.GetCollection<object>());
            dynamic collection = CallGenericMethod(db, method, baseType, fullyQualifiedName, null);

            return collection.FindById(id);
        }

        private void UpdateId(Type baseType, string fullyQualifiedName, int oldId, int newId)
        {
            var db = new LiteDb(_helpers, _databaseName);
            MethodInfo method = GetMethod<LiteDb>(q => q.GetCollection<object>());
            dynamic collection = CallGenericMethod(db, method, baseType, fullyQualifiedName, null);
            var entity = collection.FindById(oldId);
            entity.Id = newId;

            // Id CANNOT be updated in LiteDB. We have to delete and recreate instead:
            collection.Delete(oldId);
            collection.Insert(entity);

            UpdateLookups(baseType, GetListNameFromFullyQualifiedName(baseType, fullyQualifiedName), oldId, newId);
            UpdateQueueIds(fullyQualifiedName, oldId, newId);
        }

        private void UpdateQueueIds(string fullyQualifiedName, int oldId, int newId)
        {
            var commandCollection = GetCollection<Command>();
            var commands = commandCollection.Find(q => q.ItemId == oldId && q.FullyQualifiedName == fullyQualifiedName);
            foreach (var command in commands)
            {
                command.ItemId = newId;
                commandCollection.Update(command);
            }
        }

        private void UpdateLookups(Type baseType, string changedListname, int oldId, int newId)
        {
            // TODO: Update Lookups after changing Id
            var db = new LiteDb(_helpers, _databaseName);
            List<string> collectionNames = db.GetCollectionNames().ToList();
            var subTypes = baseType.Assembly.GetTypes();
            foreach (var subType in subTypes)
            {
                if (!collectionNames.Contains(subType.Name)) continue;

                PropertyInfo dynamicTargetProperty = null;
                bool modifyId = false;
                var lookupProperties = new List<PropertyInfo>();
                var virtualStaticProperties = new List<PropertyInfo>();
                var virtualDynamicProperties = new List<PropertyInfo>();

                foreach (var property in subType.GetProperties())
                {
                    bool propertyHasLookups = false;
                    var virtualLookupAttribute = property.GetCustomAttribute<VirtualLookupAttribute>();
                    var lookupAttribute = property.GetCustomAttribute<LookupAttribute>();
                    if (virtualLookupAttribute != null || lookupAttribute != null) propertyHasLookups = true;

                    if (propertyHasLookups)
                    {
                        if (virtualLookupAttribute != null)
                        {
                            if (virtualLookupAttribute.StaticTarget != null)
                            {
                                if (virtualLookupAttribute.StaticTarget != changedListname) continue;

                                modifyId = true;
                                virtualStaticProperties.Add(property);
                            }
                            else
                            {
                                if (virtualLookupAttribute.DynamicTargetProperty == null) continue;
                                dynamicTargetProperty = subType.GetProperty(virtualLookupAttribute.DynamicTargetProperty);
                                if (dynamicTargetProperty == null) continue;
                                modifyId = true;    // MIGHT be
                                virtualDynamicProperties.Add(property);
                            }
                        }
                        else if (lookupAttribute != null)
                        {
                            if (lookupAttribute.List != changedListname) continue;
                            lookupProperties.Add(property);
                            modifyId = true;
                        }
                    }
                    if (modifyId) break;
                }
                if (modifyId)
                {
                    var collection = db.GetCollection(subType.Name);
                    var elements = collection.FindAll();
                    bool elementChanged = false;
                    foreach (var element in elements)
                    {
                        foreach (var lookupProperty in lookupProperties)
                        {
                            if ((int?)element[lookupProperty.Name] == oldId)
                            {
                                element[lookupProperty.Name] = newId;
                                elementChanged = true;
                            }
                        }
                        foreach (var virtualStaticPropery in virtualStaticProperties)
                        {
                            if ((int?)element[virtualStaticPropery.Name] == oldId)
                            {
                                element[virtualStaticPropery.Name] = newId;
                                elementChanged = true;
                            }
                        }
                        foreach (var virtualDynamicProperty in virtualDynamicProperties)
                        {
                            var attribute = virtualDynamicProperty.GetCustomAttribute<VirtualLookupAttribute>();
                            if (element[attribute.DynamicTargetProperty]==changedListname)
                            {
                                if ((int?)element[virtualDynamicProperty.Name] == oldId)
                                {
                                    element[virtualDynamicProperty.Name] = newId;
                                    elementChanged = true;
                                }
                            }
                        }
                        if (elementChanged) collection.Update(element);
                    }
                }
            }
        }

        public void EmptyCommandCollection()
        {
            DropCollection<Command>(null);
        }

        public void Sync(Type baseType)
        {
            var queue = Read().Where(q => q.State == Command.States.Pending).OrderBy(q => q.Id).ToList();
            _log.Info($"Working with queue ({queue.Count} pending commands");
            foreach (var command in queue)
            {
                _log.Debug($"storing command {command}");
                string commandAction = $"Queue{command.Action}";
                try
                {
                    MethodInfo method = GetType().GetMethod(commandAction);
                    method.Invoke(this, new object[] { baseType, command });
                    command.State = Command.States.Succeeded;
                    Update(command);
                }
                catch (Exception ex)
                {
                    _log.Error($"Cannot find method for action '{commandAction}'", ex);
                    break;
                }
            }
        }
    }
}
