using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Attributes.TableAttributes
{
    public class VersioningAttribute:Attribute
    {
        public bool EnableVersioning { get; set; } = true;
        public bool EnableMinorVersioning { get; set; } = false;
        public VersioningAttribute(bool enableVersioning=true, bool enableMinorVersioning=false)
        {
            EnableVersioning = enableVersioning;
            EnableMinorVersioning = enableMinorVersioning;
        }
    }
}
