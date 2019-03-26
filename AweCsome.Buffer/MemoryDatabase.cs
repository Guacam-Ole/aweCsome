using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Buffer
{
    public class MemoryDatabase
    {
        public string Filename { get; set; }
        public bool IsQueue { get; set; }
        public LiteDB.LiteDatabase Database { get; set; }
    }
}
