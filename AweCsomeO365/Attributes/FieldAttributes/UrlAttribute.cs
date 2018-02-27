using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes.FieldAttributes
{
    public class UrlAttribute:Attribute
    {
        public UrlFieldFormatType UrlFieldFormatType { get; set; } = UrlFieldFormatType.Hyperlink;
        public  const FieldType AssociatedFieldType = FieldType.URL;
    }
}
