using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Attributes.FieldAttributes
{
    public class UserAttribute : Attribute
    {
      //  bool AllowMultipleValues { get; set; }
        public FieldUserSelectionMode FieldUserSelectionMode { get; set; } = FieldUserSelectionMode.PeopleOnly;
        public int? UserSelectionScope { get; set; }
        public const string AssociatedFieldType = nameof(FieldType.User);

    }
}
