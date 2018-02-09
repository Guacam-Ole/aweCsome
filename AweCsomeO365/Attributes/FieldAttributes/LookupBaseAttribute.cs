using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes.FieldAttributes
{
    public class LookupBaseAttribute : Attribute
    {
        public string LookupList { get; set; }
        RelationshipDeleteBehaviorType RelationshipDeleteBehaviorType { get; set; } = RelationshipDeleteBehaviorType.None;
    }
}
