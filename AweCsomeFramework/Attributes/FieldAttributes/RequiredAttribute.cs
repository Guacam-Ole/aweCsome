using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Attributes.FieldAttributes
{
    public class RequiredAttribute : Attribute
    {
        public bool IsRequired { get; set; }
        public RequiredAttribute(bool isRequired)
        {
            IsRequired = isRequired;
        }
        public RequiredAttribute() : this(true) { }
    }
}
