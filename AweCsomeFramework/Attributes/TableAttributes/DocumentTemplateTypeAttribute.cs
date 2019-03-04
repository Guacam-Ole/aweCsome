using System;

namespace AweCsome.Attributes.TableAttributes
{
    public class DocumentTemplateTypeAttribute : Attribute
    {
        public int DocumentTemplateTypeId { get; set; }

        public DocumentTemplateTypeAttribute(int documentTemplateTypeId)
        {
            DocumentTemplateTypeId = documentTemplateTypeId;
        }


    }
}
