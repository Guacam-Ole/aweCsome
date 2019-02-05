using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Attributes.TableAttributes
{
    public class DocumentTemplateTypeAttribute:Attribute
    {
        public int DocumentTemplateTypeId { get; set; }

        public DocumentTemplateTypeAttribute(int documentTemplateTypeId)
        {
            DocumentTemplateTypeId = documentTemplateTypeId;
        }
     

    }
}
