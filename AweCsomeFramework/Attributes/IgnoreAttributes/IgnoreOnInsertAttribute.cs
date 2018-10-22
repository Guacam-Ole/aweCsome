using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes.IgnoreAttributes
{
    public class IgnoreOnInsertAttribute:Attribute
    {
        public bool IgnoreOnInsert { get; set; } = true;
    }
}
