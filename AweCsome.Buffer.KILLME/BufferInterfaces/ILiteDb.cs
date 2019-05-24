using System.Collections.Generic;
using System.IO;
using AweCsome.Entities;

namespace AweCsome.Buffer.BufferInterfaces
{
    public interface ILiteDb
    {
        void DeleteTable(string name);
        void RemoveAttachment(BufferFileMeta meta);
        List<string> GetAttachmentNamesFromItem<T>(int id);
        List<string> GetFilenamesFromLibrary<T>(string folder);
        Dictionary<string, Stream> GetAttachmentsFromItem<T>(int id);
        List<AweCsomeLibraryFile> GetFilesFromDocLib<T>(string folder);
        void AddAttachment(BufferFileMeta meta, Stream fileStream);
        int Insert<T>(T item, string listname);
        LiteDB.LiteCollection<T> GetCollection<T>();
    }
}
