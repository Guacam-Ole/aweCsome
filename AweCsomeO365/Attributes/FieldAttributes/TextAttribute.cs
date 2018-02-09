using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes.FieldAttributes
{
    public class TextAttribute : Attribute
    {
        public int MaxCharacters { get; set; } = 255;
        public string DefaultValue { get; set; }

    }
}
