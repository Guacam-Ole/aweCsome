using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Hosting;
using AweCsome.Interfaces;

namespace AweCsome.Buffer
{
    public class LiteDb
    {
        private enum DbModes { Memory, File, Undefined };
        private DbModes _dbMode = DbModes.Undefined;
        private static List<MemoryDatabase> _memoryDb = new List<MemoryDatabase>();
        private static object _dbLock = new object();
        private LiteDB.LiteDatabase _database;
        private IAweCsomeHelpers _helpers;

        public LiteDb(IAweCsomeHelpers helpers, string databaseName, bool queue)
        {
            if (queue) databaseName += ".QUEUE";
            _database = GetDatabase(databaseName, queue);
            _helpers = helpers;
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
           
            var collection = GetCollection<T>(listname);
            int minId = collection.Min().AsInt32;
            if (minId > 0) minId = 0;
            minId--;
            _helpers.SetId<T>(item, minId);

            return collection.Insert(item);
        }

  

        public LiteDB.LiteCollection<T> GetCollection<T>()
        {
            return _database.GetCollection<T>();
        }

        private string CreateConnectionString(string databasename)
        {
            return HostingEnvironment.MapPath("/db/"+databasename.Replace("https", "").Replace("http", "").Replace(":", "").Replace("/", "") + ".test.liteDB");
        }

        private LiteDB.LiteDatabase GetDatabase(string databaseName, bool isQueue)
        {
            if (_dbMode == DbModes.Undefined)
            {
                string dbModeSetting = ConfigurationManager.AppSettings["DbMode"];
                if (dbModeSetting == null)
                {
                    _dbMode = DbModes.File;
                }
                else
                {
                    _dbMode = DbModes.Memory;
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
                    return new LiteDB.LiteDatabase(CreateConnectionString(databaseName));
                }
            }
        }
    }
}
