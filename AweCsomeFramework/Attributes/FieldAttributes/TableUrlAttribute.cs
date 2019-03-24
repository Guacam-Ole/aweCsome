using System;
using AweCsome.Enumerations;

namespace AweCsome.Attributes.FieldAttributes
{
    public class TableUrlAttribute : Attribute
    {
        public UrlFieldFormatType UrlFieldFormatType { get; set; } = UrlFieldFormatType.Hyperlink;
        public const string AssociatedFieldType = "URL";
    }
}
