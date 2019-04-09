using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using AweCsome.Interfaces;

namespace AweCsome.Buffer
{
    public class LiteDb
    {
        private Random random = new Random();
        private const string RandomChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        private const string PrefixAttachment = "UploadAttachment_";
        private const string PrefixFile = "UploadFile_";
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

        private string GetStringIdFromFilename(string prefix, string listname, int id, string filename)
        {
            return $"{prefix}_{listname}_{id}_{Rand()}_{filename}";
        }

        private string Rand(int length = 8)
        {
            return new string(Enumerable.Repeat(RandomChars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        protected LiteDB.LiteCollection<T> GetCollection<T>(string name)
        {
            name = name ?? typeof(T).Name;
            return _database.GetCollection<T>(name);
        }

        private LiteDB.LiteStorage GetStorage()
        {
            return _database.FileStorage;
        }


        public void RemoveAttachmentFromItem(BufferFileMeta meta)
        {
            RemoveAttachmentFromItemOrDocLib(PrefixAttachment, meta);
        }

        public void RemoveFileFromDocLib(BufferFileMeta meta)
        {
            RemoveAttachmentFromItemOrDocLib(PrefixFile, meta);
        }

        private void RemoveAttachmentFromItemOrDocLib(string prefix, BufferFileMeta meta)
        {
            var existingFile = _database.FileStorage.Find(GetStringIdFromFilename(prefix, meta.Listname, meta.ParentId, meta.Filename)).FirstOrDefault();
            if (existingFile == null) return;
            _database.FileStorage.Delete(existingFile.Id);
        }
        public void AddAttachmentToItem(BufferFileMeta meta, Stream fileStream)
        {
            AddImageOrAttachment(PrefixAttachment, meta, fileStream);
        }

        public void AddFileToDocLib(BufferFileMeta meta, Stream fileStream)
        {
            AddImageOrAttachment(PrefixFile, meta, fileStream);
        }

        private LiteDB.BsonDocument GetMetadataFromAttachment(BufferFileMeta meta)
        {
            var doc = new LiteDB.BsonDocument();
            doc[nameof(BufferFileMeta.AttachmentType)] = meta.AttachmentType.ToString();
            doc[nameof(BufferFileMeta.Filename)] = meta.Filename;
            doc[nameof(BufferFileMeta.Folder)] = meta.Folder;
            doc[nameof(BufferFileMeta.Id)] = meta.Id;
            doc[nameof(BufferFileMeta.Listname)] = meta.Listname;
            doc[nameof(BufferFileMeta.ParentId)] = meta.ParentId;

            return doc;
        }

        private void AddImageOrAttachment(string prefix, BufferFileMeta meta, Stream fileStream)
        {
            int calculatedIndex = 0;
            var existingFiles = _database.FileStorage.Find(prefix);
            if (existingFiles.Count() > 0)
            {
                calculatedIndex = existingFiles.Min(q => (int?)q.Metadata["Id"]) ?? 0;
                if (calculatedIndex > 0) calculatedIndex = 0;
            }
            calculatedIndex--;
            meta.SetId(calculatedIndex);
            var uploadedFile = _database.FileStorage.Upload(GetStringIdFromFilename(prefix, meta.Listname, meta.ParentId, meta.Filename), meta.Filename, fileStream);
            _database.FileStorage.SetMetadata(uploadedFile.Id, GetMetadataFromAttachment(meta));
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
            return HostingEnvironment.MapPath("/db/" + databasename.Replace("https", "").Replace("http", "").Replace(":", "").Replace("/", "") + ".test.liteDB");
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
