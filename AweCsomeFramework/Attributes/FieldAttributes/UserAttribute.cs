using System;
using AweCsome.Enumerations;

namespace AweCsome.Attributes.FieldAttributes
{
    public class UserAttribute : Attribute
    {
        public FieldUserSelectionMode FieldUserSelectionMode { get; set; } = FieldUserSelectionMode.PeopleOnly;
        public int? UserSelectionScope { get; set; }
        public const string AssociatedFieldType = "User";

    }
}
