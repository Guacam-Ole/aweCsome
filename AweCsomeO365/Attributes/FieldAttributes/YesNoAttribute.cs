using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes.FieldAttributes
{
    public class YesNoAttribute:Attribute
    {
        public bool DefaultValue { get; set; }
    }
}
