using System;

namespace AweCsome.Attributes.FieldAttributes
{
    public class ChoiceAttribute : Attribute
    {
        public enum DisplayChoicesTypes { Dropdown, RadioButtons, CheckBoxes }
        public string[] Choices { get; set; }
        public DisplayChoicesTypes DisplayChoices { get; set; }
        public bool AllowFillIn { get; set; }
        public string DefaultValue { get; set; }
        public const string AssociatedFieldType = "Choice";

        public ChoiceAttribute() { }
        public ChoiceAttribute(Type enumType)
        {
            Choices = Enum.GetNames(enumType);
        }
    }
}
