using System;

namespace AweCsome.Attributes.FieldAttributes
{
    public class BooleanAttribute : Attribute
    {
        public bool DefaultValue { get; set; }
        public const string AssociatedFieldType = "Boolean";
    }
}
