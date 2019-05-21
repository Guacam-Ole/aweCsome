using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using AweCsome.Entities;
using AweCsome.Interfaces;
using LiteDB;

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
        protected IAweCsomeHelpers _helpers;
        protected string _databaseName;

        public LiteDb(IAweCsomeHelpers helpers, string databaseName, bool queue = false)
        {
            _databaseName = databaseName;
            if (queue) databaseName += ".QUEUE";
            _database = GetDatabase(databaseName, queue);
            _helpers = helpers;
            RegisterMappers();
        }

        private void RegisterMappers()
        {
            BsonMapper.Global.RegisterType<KeyValuePair<int, string>>(
                serialize: (pair) => $"{pair.Key}[-]{pair.Value}",
                deserialize: (bson) => new KeyValuePair<int, string>(int.Parse(bson.AsString.Split(new string[] { "[-]" },StringSplitOptions.None)[0]), bson.AsString.Split(new string[] { "[-]" }, StringSplitOptions.None)[1])
                );
        }

        public void DeleteTable(string name)
        {
            _database.DropCollection(name);
        }

        private string CleanUpLiteDbId(string dirtyName)
        {
            return dirtyName.Replace("/", "").Replace("\\", "").Replace("-", "_");
        }

        private string GetStringIdFromFilename(BufferFileMeta meta, bool pathOnly = false)
        {
            string stringId = $"{meta.AttachmentType}_{meta.Listname}_{meta.Folder}_{meta.ParentId}_";
            if (!pathOnly) stringId += "{Rand()}_{meta.Filename}";
            return CleanUpLiteDbId(stringId);
        }

        private string Rand(int length = 8)
        {
            return new string(Enumerable.Repeat(RandomChars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        protected void DropCollection<T>(string name)
        {
            name = name ?? typeof(T).Name;
            _database.DropCollection(name);
        }

        protected LiteDB.LiteCollection<T> GetCollection<T>(string name)
        {
            
            name = name ?? typeof(T).Name;
            return _database.GetCollection<T>(name);
        }

        public LiteCollection<BsonDocument> GetCollection(string name)
        {
            return  _database.GetCollection(name);
        }

        private LiteDB.LiteStorage GetStorage()
        {
            return _database.FileStorage;
        }

        public void RemoveAttachment(BufferFileMeta meta)
        {
            var existingFile = _database.FileStorage.Find(GetStringIdFromFilename(meta)).FirstOrDefault();
            if (existingFile == null) return;
            _database.FileStorage.Delete(existingFile.Id);
        }

        public List<string> GetAttachmentNamesFromItem<T>(int id)
        {
            var matches = new List<string>();
            string prefix = GetStringIdFromFilename(new BufferFileMeta { AttachmentType = BufferFileMeta.AttachmentTypes.Attachment, ParentId = id, Listname = _helpers.GetListName<T>() }, true);
            var files = _database.FileStorage.Find(prefix);
            if (matches == null) return null;
            foreach (var file in files)
            {
                matches.Add(file.Filename);
            }
            return matches;
        }

        public List<string> GetFilenamesFromLibrary<T>(string folder)
        {
            var matches = new List<string>();

            string prefix = GetStringIdFromFilename(new BufferFileMeta { AttachmentType = BufferFileMeta.AttachmentTypes.DocLib, Folder = folder, Listname = _helpers.GetListName<T>() }, true);

            var files = _database.FileStorage.Find(prefix);
            foreach (var file in files)
            {
                matches.Add(file.Filename);
            }
            return matches;
        }

        public Dictionary<string, Stream> GetAttachmentsFromItem<T>(int id)
        {
            var matches = new Dictionary<string, Stream>();
            string prefix = GetStringIdFromFilename(new BufferFileMeta { AttachmentType = BufferFileMeta.AttachmentTypes.Attachment, ParentId = id, Listname = _helpers.GetListName<T>() }, true);
            var files = _database.FileStorage.Find(prefix);
            if (matches == null) return null;
            foreach (var file in files)
            {
                MemoryStream fileStream = new MemoryStream((int)file.Length);
                file.CopyTo(fileStream);
                matches.Add(file.Filename, fileStream);
            }
            return matches;
        }

        public List<AweCsomeLibraryFile> GetFilesFromDocLib<T>(string folder)
        {
            var matches = new List<AweCsomeLibraryFile>();

            string prefix = GetStringIdFromFilename(new BufferFileMeta { AttachmentType = BufferFileMeta.AttachmentTypes.DocLib, Folder = folder, Listname = _helpers.GetListName<T>() }, true);

            var files = _database.FileStorage.Find(prefix);
            foreach (var file in files)
            {
                MemoryStream fileStream = new MemoryStream((int)file.Length);
                file.CopyTo(fileStream);
                matches.Add(new AweCsomeLibraryFile
                {
                    Stream = fileStream,
                    Filename = file.Filename,
                    Entity = file.Metadata
                });
            }
            return matches;
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
            doc[nameof(BufferFileMeta.AdditionalInformation)] = null; // meta.AdditionalInformation; // TODO; Serialize AdditionalInformation property

            return doc;
        }

        public void AddAttachment(BufferFileMeta meta, Stream fileStream)
        {
            int calculatedIndex = 0;
            string prefix = GetStringIdFromFilename(meta, true);
            var existingFiles = _database.FileStorage.Find(prefix);
            if (existingFiles.Count() > 0)
            {
                calculatedIndex = existingFiles.Min(q => (int?)q.Metadata["Id"]) ?? 0;
                if (calculatedIndex > 0) calculatedIndex = 0;
            }
            calculatedIndex--;
            meta.SetId(calculatedIndex);
            var uploadedFile = _database.FileStorage.Upload(GetStringIdFromFilename(meta), meta.Filename, fileStream);
            _database.FileStorage.SetMetadata(uploadedFile.Id, GetMetadataFromAttachment(meta));
        }

        public int Insert<T>(T item, string listname)
        {

            var collection = GetCollection<T>(listname);
            int minId = collection.Min("Id").AsInt32;
            if (minId > 0) minId = 0;
            minId--;
            _helpers.SetId<T>(item, minId);

           return  collection.Insert(item);
        }

        public LiteDB.LiteCollection<T> GetCollection<T>()
        {
            return _database.GetCollection<T>();
        }

        public IEnumerable<string> GetCollectionNames()
        {
            return _database.GetCollectionNames();
        }

        private string CreateConnectionString(string databasename)
        {
            string localPath = HostingEnvironment.MapPath("/db/" + databasename);
            if (localPath == null)
            {
                // No Web environment
                localPath = System.Environment.CurrentDirectory + "\\" + databasename;
            }
            else
            {
                //     localPath = localPath.Replace("https", "").Replace("http", "").Replace(":", "").Replace("/", "");
            }
            return "Filename=" + localPath;
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
