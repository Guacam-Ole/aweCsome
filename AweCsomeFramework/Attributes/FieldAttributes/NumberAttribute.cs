using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes.FieldAttributes
{
    public class NumberAttribute : Attribute
    {
        public double? Min { get; set; }
        public double? Max { get; set; }
        public int? NumberOfDecimalPlaces { get; set; }
        public double? DefaultValue { get; set; }
        public bool ShowAsPercentage { get; set; }
        public const FieldType AssociatedFieldType = FieldType.Number;
    }
}
