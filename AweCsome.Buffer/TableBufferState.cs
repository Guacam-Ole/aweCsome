using System;

namespace AweCsome.Buffer
{
    public class TableBufferState
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Size { get; set; }
        public DateTime? LastDeletionDate { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        public DateTime? LastInsertDate { get; set; }
        public DateTime Validated { get; set; }
    }
}
