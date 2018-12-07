using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Attributes.FieldAttributes
{
    public class BooleanAttribute : Attribute
    {
        public bool DefaultValue { get; set; }
        public  const string AssociatedFieldType = nameof(FieldType.Boolean);
    }
}
