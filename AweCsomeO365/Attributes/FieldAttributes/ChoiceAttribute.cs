using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes.FieldAttributes
{
    public class ChoiceAttribute : Attribute
    {
        public enum DisplayChoicesTypes { DropDown, RadioButtons, CheckBoxes }
        public string[] Choices { get; set; }
        public DisplayChoicesTypes DisplayChoices { get; set; }
        public bool AllowFillIn { get; set; }
        public string DefaultValue { get; set; }

    }
}
