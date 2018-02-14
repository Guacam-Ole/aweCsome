using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes.FieldAttributes
{
    public class DateTimeAttribute:Attribute
    {
        public DateTimeFormat  DateTimeFormat { get; set; }
        public DateTimeFieldFriendlyFormatType DateTimeFieldFriendlyFormatType { get; set; }
        public DateTime? DefaultValue { get; set; }
        public  const FieldType AssociatedFieldType = FieldType.DateTime;

    }
}
