using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Attributes.IgnoreAttributes
{
    public class IgnoreOnSelectAttribute:Attribute
    {
        public bool IgnoreOnSelect { get; set; } = true;
    }
}
