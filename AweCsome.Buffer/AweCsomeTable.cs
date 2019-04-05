using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AweCsome.Entities;
using AweCsome.Interfaces;

namespace AweCsome.Buffer
{
    public class AweCsomeTable : IAweCsomeTable
    {
        private IAweCsomeTable _baseTable;
        private IAweCsomeHelpers _helpers;
        private LiteDb _db;
        private LiteDbQueue _queue;

        public AweCsomeTable(IAweCsomeTable baseTable, IAweCsomeHelpers helpers, string databasename)
        {
            _baseTable = baseTable;
            _helpers = helpers;
            _db = new LiteDb(databasename,false);
            _queue = new LiteDbQueue(databasename,true);
        }

        public string AddFolderToLibrary<T>(string folder)
        {
            return _baseTable.AddFolderToLibrary<T>(folder);   // NOT buffered
        }

        public void AttachFileToItem<T>(int id, string filename, Stream filestream)
        {
            throw new NotImplementedException();
        }

        public string AttachFileToLibrary<T>(string folder, string filename, Stream filestream, T entity)
        {
            throw new NotImplementedException();
        }

        public int CountItems<T>()
        {
            return _db.Count(_helpers.GetListName<T>());
        }

        public int CountItemsByFieldValue<T>(string fieldname, object value)
        {
            throw new NotImplementedException();
        }

        public int CountItemsByMultipleFieldValues<T>(Dictionary<string, object> conditions, bool isAndCondition = true)
        {
            throw new NotImplementedException();
        }

        public int CountItemsByQuery<T>(string query)
        {
            throw new NotImplementedException();
        }
 
        public void DeleteFileFromItem<T>(int id, string filename)
        {
            throw new NotImplementedException();
        }

        public void DeleteFilesFromDocumentLibrary<T>(string path, List<string> filenames)
        {
            throw new NotImplementedException();
        }

        public void DeleteFolderFromDocumentLibrary<T>(string path, string folder)
        {
            throw new NotImplementedException();
        }

        public void DeleteItemById<T>(int id)
        {
            throw new NotImplementedException();
        }

        public void DeleteTable<T>()
        {
            _baseTable.DeleteTable<T>();
            BufferState.RemoveTable(_helpers.GetListName<T>());
        }

        public void DeleteTableIfExisting<T>()
        {
            _baseTable.DeleteTableIfExisting<T>();
            BufferState.RemoveTable(_helpers.GetListName<T>());
        }

        public void Empty<T>()
        {
            throw new NotImplementedException();
        }

        public string[] GetAvailableChoicesFromField<T>(string propertyname)
        {
            throw new NotImplementedException();
        }

        public int InsertItem<T>(T entity)
        {
            string listname = _helpers.GetListName<T>();
            int itemId=_db.Insert(entity, listname);
            _queue.QueueAddCommand(new Command
            {
                Action = Command.Actions.Insert,
                ItemId = itemId,
                TableName = listname,
                State = Command.States.Pending
            });
            return itemId ;
        }

        public T Like<T>(int id, int userId) where T : new()
        {
            throw new NotImplementedException();
        }

        public List<T> SelectAllItems<T>() where T : new()
        {
            throw new NotImplementedException();
        }

        public AweCsomeLibraryFile SelectFileFromLibrary<T>(string foldername, string filename) where T : new()
        {
            throw new NotImplementedException();
        }

        public List<string> SelectFileNamesFromItem<T>(int id)
        {
            throw new NotImplementedException();
        }

        public List<string> SelectFileNamesFromLibrary<T>(string foldername)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, Stream> SelectFilesFromItem<T>(int id)
        {
            throw new NotImplementedException();
        }

        public List<AweCsomeLibraryFile> SelectFilesFromLibrary<T>(string foldername) where T : new()
        {
            throw new NotImplementedException();
        }

        public T SelectItemById<T>(int id) where T : new()
        {
            throw new NotImplementedException();
        }

        public List<T> SelectItemsByFieldValue<T>(string fieldname, object value) where T : new()
        {
            throw new NotImplementedException();
        }

        public List<T> SelectItemsByMultipleFieldValues<T>(Dictionary<string, object> conditions, bool isAndCondition = true) where T : new()
        {
            throw new NotImplementedException();
        }

        public List<T> SelectItemsByQuery<T>(string query) where T : new()
        {
            throw new NotImplementedException();
        }

        public List<T> SelectItemsByTitle<T>(string title) where T : new()
        {
            throw new NotImplementedException();
        }

        public T Unlike<T>(int id, int userId) where T:new()
        {
            throw new NotImplementedException();
        }

        public void UpdateItem<T>(T entity)
        {
            throw new NotImplementedException();
        }

        public Guid CreateTable<T>()
        {
            Guid newId = _baseTable.CreateTable<T>();
            BufferState.AddTable(_helpers.GetListName<T>(), newId);
            return newId;
        }

        public Dictionary<string, Stream> SelectFilesFromItem<T>(int id, string filename = null)
        {
            throw new NotImplementedException();
        }
    }
}
