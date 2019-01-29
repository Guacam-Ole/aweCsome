using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Entities
{
    public class AweCsomeLibraryFile
    {
        public string Filename { get; set; }
        public Stream Stream { get; set; }
        public object entity { get; set; }
    }
}
