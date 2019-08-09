using System;

namespace AweCsome.Attributes.FieldAttributes
{
    public class TextAttribute : Attribute
    {
        public int MaxCharacters { get; set; } = 255;
        public string DefaultValue { get; set; }
        public const string AssociatedFieldType = "Text";

    }
}
