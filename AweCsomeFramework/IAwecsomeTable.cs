﻿using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome
{
    public interface IAweCsomeTable
    {
        void CreateTable<T>();
        void DeleteTable<T>();
        void DeleteTableIfExisting<T>();
        int InsertItem<T>(T entity);
        T SelectItemById<T>(int id) where T : new();
        List<T> SelectAllItems<T>() where T : new();
        List<T> SelectItemsByFieldValue<T>(string fieldname, object value) where T : new();
        List<T> SelectItemsByMultipleFieldValues<T>(Dictionary<string, object> conditions) where T : new();
        List<T> SelectItemsByQuery<T>(string query) where T : new();
        void UpdateItem<T>(T entity);
        void DeleteItemById<T>(int id);
        string[] GetAvailableChoicesFromField<T>(string propertyname);
        void Like<T>(int id, int userId);
        void Unlike<T>(int id, int userId);
        List<string> SelectFileNamesFromItem<T>(int id);
        Dictionary<string, Stream> SelectFilesFromItem<T>(int id);
        void AttachFileToItem<T>(int id, string filename, Stream filestream);
        void DeleteFileFromItem<T>(int id, string filename);

        string AttachFileToLibrary<T>(string folder, string filename, Stream filestream, T entity );
        Dictionary<string, Stream> SelectFilesFromLibrary<T>(string folder);
        string AddFolderToLibrary<T>(string folder);

        int CountItems<T>();
        int CountItemsByFieldValue<T>(string fieldname, object value);
        int CountItemsByMultipleFieldValues<T>(Dictionary<string, object> conditions);
        int CountItemsByQuery<T>(string query);
    }
}
