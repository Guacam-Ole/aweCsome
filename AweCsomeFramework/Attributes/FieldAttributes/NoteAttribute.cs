using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Attributes.FieldAttributes
{
    public class NoteAttribute: Attribute
    {
        public int NumberOfLinesForEditing { get; set; } = 6;
        public bool AllowRichText { get; set; } = true;
        public bool AppendChangesToExistingText { get; set; }
        public  const string AssociatedFieldType = nameof(FieldType.Note);

    }
}
