using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Buffer
{
    public class Command
    {
        public enum Actions { DeleteTable, CreateTable, Insert, Update, Delete, SendMail, Empty, UploadAttachment, RemoveAttachment, UploadFile, RemoveFile }

        public enum States { Pending, Failed, Succeeded, Delayed}

        public Actions Action { get; set; }
        public object[] Parameters { get; set; }
        public Guid Id { get;  } = Guid.NewGuid();  // Default ID for LiteDB
        public string TableName { get; set; }
        public States State { get; set; } = States.Pending;
        public int? ItemId { get; set; } 
        public DateTime Created { get; } = DateTime.Now;
    }
}
