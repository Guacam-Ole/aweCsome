using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes.FieldAttributes
{
    public class UniqueAttribute:Attribute
    {
        public bool IsUnique { get; set; } = true;
    }
}
