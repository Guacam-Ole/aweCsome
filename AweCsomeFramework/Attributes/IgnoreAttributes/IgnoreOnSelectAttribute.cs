using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes.IgnoreAttributes
{
    public class IgnoreOnSelectAttribute:Attribute
    {
        public bool IgnoreOnSelect { get; set; } = true;
    }
}
