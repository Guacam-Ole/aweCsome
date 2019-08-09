using System;

namespace AweCsome.Attributes.FieldAttributes
{
    public class NoteAttribute : Attribute
    {
        public int NumberOfLinesForEditing { get; set; } = 6;
        public bool AllowRichText { get; set; } = true;
        public bool AppendChangesToExistingText { get; set; }
        public const string AssociatedFieldType = "Note";
    }
}
