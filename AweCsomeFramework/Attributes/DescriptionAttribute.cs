using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes
{
    public class DescriptionAttribute:Attribute
    {
        public string Description { get; set; }
        public DescriptionAttribute(string description)
        {
            Description = description;
        }

    }
}
