using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Buffer.Attributes
{
    public class VirtualLookupAttribute:Attribute
    {
        public string StaticTarget { get; set; }
        public string DynamicTargetProperty { get; set; }
    }
}
