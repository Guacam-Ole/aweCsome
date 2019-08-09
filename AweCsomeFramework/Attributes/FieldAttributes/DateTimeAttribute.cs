using System;
using AweCsome.Enumerations;

namespace AweCsome.Attributes.FieldAttributes
{
    public class DateTimeAttribute : Attribute
    {
        public DateTimeFormat DateTimeFormat { get; set; }
        public DateTimeFieldFriendlyFormatType DateTimeFieldFriendlyFormatType { get; set; }
        public string DefaultValue { get; set; }
        public const string AssociatedFieldType = "DateTime";

    }
}
