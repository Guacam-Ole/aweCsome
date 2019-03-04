using System;

namespace AweCsome.Attributes.FieldAttributes
{
    public class CurrencyAttribute : Attribute
    {
        public double? Min { get; set; }
        public double? Max { get; set; }
        public int? NumberOfDecimalPlaces { get; set; }
        public int? CurrencyLocaleId { get; set; }
        public const string AssociatedFieldType = "Currency";
    }
}
