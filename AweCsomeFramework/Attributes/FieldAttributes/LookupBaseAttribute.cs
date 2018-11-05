using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Attributes.FieldAttributes
{
    public class LookupBaseAttribute : Attribute
    {
        public string List { get; set; }
        public string Field { get; set; } = "Title";
        RelationshipDeleteBehaviorType RelationshipDeleteBehaviorType { get; set; } = RelationshipDeleteBehaviorType.None;
        public  const FieldType AssociatedFieldType = FieldType.Lookup;
    }
}
