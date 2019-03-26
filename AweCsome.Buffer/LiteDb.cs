using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace AweCsome.Buffer
{
    public class LiteDb
    {
        private enum DbModes { Memory, File, Undefined };
        private DbModes _dbMode = DbModes.Undefined;
        private static List<MemoryDatabase> _memoryDb = new List<MemoryDatabase>();
        private static object _dbLock = new object();
        private LiteDB.LiteDatabase _database;

        public LiteDb(string databaseName, bool queue)
        {
            if (queue) databaseName += ".QUEUE";
            _database = GetDatabase(databaseName, queue);
        }


        public void DeleteTable(string name)
        {
            _database.DropCollection(name);
        }

        protected LiteDB.LiteCollection<T> GetCollection<T>(string name)
        {
            name = name ?? typeof(T).Name;
            return _database.GetCollection<T>(name);
        }

        public int Insert<T>(T item, string listname)
        {
            return GetCollection<T>(listname).Insert(item);
        }

        public int Count(string name)
        {
            return _database.GetCollection(name).Count();
        }

        private string CreateConnectionString()
        {
            throw new NotImplementedException();
        }

        private LiteDB.LiteDatabase GetDatabase(string databaseName, bool isQueue)
        {
            if (_dbMode == DbModes.Undefined)
            {
                string dbModeSetting = ConfigurationManager.AppSettings["DbMode"];
                if (dbModeSetting == null)
                {
                    _dbMode = DbModes.Memory;
                }
                else
                {
                    _dbMode = DbModes.File;
                }
            }
            lock (_dbLock)
            {
                if (_dbMode == DbModes.Memory)
                {

                    var oldDb = _memoryDb.FirstOrDefault(q => q.Filename == databaseName);
                    if (oldDb == null) _memoryDb.Add(new MemoryDatabase { Filename = databaseName, IsQueue = isQueue, Database = new LiteDB.LiteDatabase(new MemoryStream()) });
                    return _memoryDb.First(q => q.Filename == databaseName).Database;
                }
                else
                {
                    return new LiteDB.LiteDatabase(CreateConnectionString());
                }
            }
        }
    }
}
