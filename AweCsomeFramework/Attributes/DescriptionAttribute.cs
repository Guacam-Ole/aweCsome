using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Attributes
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
