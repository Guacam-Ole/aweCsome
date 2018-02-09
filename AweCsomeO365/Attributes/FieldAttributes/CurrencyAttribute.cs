using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes.FieldAttributes
{
    public class CurrencyAttribute:Attribute
    {
        public double? Min { get; set; }
        public double? Max { get; set; }
        public int? NumberOfDecimalPlaces { get; set; }
        public int? CurrencyLocaleId { get; set; }
    }
}
