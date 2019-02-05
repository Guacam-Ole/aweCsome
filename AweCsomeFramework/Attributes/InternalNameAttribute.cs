using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Attributes
{
    public class InternalNameAttribute:Attribute
    {
        public string InternalName { get; set; }
        public InternalNameAttribute(string internalName)
        {
            InternalName = internalName;
        }
    }
}
