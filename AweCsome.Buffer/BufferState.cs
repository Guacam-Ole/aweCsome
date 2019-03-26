using System;
using System.Collections.Generic;
using System.Linq;

namespace AweCsome.Buffer
{
    public static class BufferState
    {
     
        public static List<TableBufferState> TableBuffers { get; } = new List<TableBufferState>();

        public static void AddTable(string tableName, Guid id)
        {
            RemoveTable(tableName);

            TableBuffers.Add(new TableBufferState
            {
                Id = id,
                Name = tableName,
                Size = 0,
                Validated = DateTime.Now
            });
        }

        public static void RemoveTable(string tableName)
        {
            var existingTable = TableBuffers.FirstOrDefault(q => q.Name == tableName);
            if (existingTable != null) TableBuffers.Remove(existingTable);
        }

    }
}
