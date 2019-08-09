using System;
using AweCsome.Enumerations;

namespace AweCsome.Attributes.FieldAttributes
{
    public class LookupBaseAttribute : Attribute
    {
        public string List { get; set; }
        public string Field { get; set; } = "Title";
        RelationshipDeleteBehaviorType RelationshipDeleteBehaviorType { get; set; } = RelationshipDeleteBehaviorType.None;
        public const string AssociatedFieldType = "Lookup";
    }
}
