using System;
using System.Collections.Generic;
using System.IO;
using AweCsome.Entities;

namespace AweCsome.Interfaces
{
    public interface IAweCsomeTable
    {
        List<T> SelectItemsByTitle<T>(string title) where T : new();
        Guid CreateTable<T>();
        void DeleteTable<T>();
        void DeleteTableIfExisting<T>();
        int InsertItem<T>(T entity);
        T SelectItemById<T>(int id) where T : new();
        List<T> SelectAllItems<T>() where T : new();
        List<T> SelectItemsByFieldValue<T>(string fieldname, object value) where T : new();
        List<T> SelectItemsByMultipleFieldValues<T>(Dictionary<string, object> conditions, bool isAndCondition = true) where T : new();
        List<T> SelectItemsByQuery<T>(string query) where T : new();
        void UpdateItem<T>(T entity);
        void DeleteItemById<T>(int id);
        void Empty<T>();
        string[] GetAvailableChoicesFromField<T>(string propertyname);
        T Like<T>(int id, int userId) where T : new();
        T Unlike<T>(int id, int userId) where T : new();
        List<string> SelectFileNamesFromItem<T>(int id);
        Dictionary<string, Stream> SelectFilesFromItem<T>(int id, string filename = null);
        void AttachFileToItem<T>(int id, string filename, Stream filestream);
        void DeleteFileFromItem<T>(int id, string filename);
        string AttachFileToLibrary<T>(string folder, string filename, Stream filestream, T entity);
        List<AweCsomeLibraryFile> SelectFilesFromLibrary<T>(string foldername, bool retrieveContent = true) where T : new();
        AweCsomeLibraryFile SelectFileFromLibrary<T>(string foldername, string filename) where T : new();
        List<string> SelectFileNamesFromLibrary<T>(string foldername);
        string AddFolderToLibrary<T>(string folder);
        int CountItems<T>();
        int CountItemsByFieldValue<T>(string fieldname, object value) where T : new();
        int CountItemsByMultipleFieldValues<T>(Dictionary<string, object> conditions, bool isAndCondition = true);
        int CountItemsByQuery<T>(string query);
        void DeleteFilesFromDocumentLibrary<T>(string path, List<string> filenames);
        void DeleteFolderFromDocumentLibrary<T>(string path, string folder);
        bool HasChangesSince<T>(DateTime compareDate) where T : new();
        List<KeyValuePair<AweCsomeListUpdate, T>> ModifiedItemsSince<T>(DateTime compareDate) where T : new();
        bool Exists<T>();
        void UpdateTableStructure<T>();


    }
}
