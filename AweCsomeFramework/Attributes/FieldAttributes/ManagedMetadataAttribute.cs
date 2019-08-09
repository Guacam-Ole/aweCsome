using System;

namespace AweCsome.Attributes.FieldAttributes
{
    public class ManagedMetadataAttribute : Attribute
    {
        public Guid TermSetId { get; set; }
        public string TermSetName { get; set; }
        public bool CreateIfMissing { get; set; } = true;
        public bool AllowFillIn { get; set; }
        public const string AssociatedFieldType = "TaxonomyFieldType";
    }
}
