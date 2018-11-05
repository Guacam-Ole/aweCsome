//using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Attributes.TableAttributes
{
    public class ListTemplateAttribute : Attribute
    {
        public int TemplateTypeId { get; set; }

        public ListTemplateAttribute(int templateTypeId)
        {
            TemplateTypeId = templateTypeId;
        }

        //public ListTemplateAttribute(ListTemplateType listTemplateType)
        //{
        //    TemplateTypeId = (int)listTemplateType;
        //}
    }
}
