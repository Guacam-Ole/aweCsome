//using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Attributes.TableAttributes
{
    public class ListTemplateTypeAttribute : Attribute
    {
        public int TemplateTypeId { get; set; }

        public ListTemplateTypeAttribute(int templateTypeId)
        {
            TemplateTypeId = templateTypeId;
        }
    }
}
