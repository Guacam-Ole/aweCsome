using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Entities
{
    public class AweCsomeListUpdate
    {
        public enum ChangeTypes { Add, Delete, Update };
        public ChangeTypes ChangeType { get; set; }
        public DateTime ChangeDate { get; set; }
        public int Id { get; set; }

    }
}
