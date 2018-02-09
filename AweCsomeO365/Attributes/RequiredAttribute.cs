using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes
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
