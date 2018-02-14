using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes.FieldAttributes
{
    public class ManagedMetadataAttribute: Attribute
    {
        public bool AllowMultipleValues { get; set; }
        public Guid TermSetId { get; set; }
        public string TermSetName { get; set; }
        public bool AllowFillIn { get; set; }
        public  const FieldType AssociatedFieldType = FieldType.Lookup;
    }
}
