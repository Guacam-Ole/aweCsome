using Microsoft.SharePoint.Client.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome
{
    interface IAweCsomeTaxonomy
    {
        void GetTaxonomyFieldInfo(string termsetName, bool createIfNotExisting, out Guid termStoreId, out Guid termSetId);
        void GetTermSet(string termsetName, int lcid, bool createIfMissing, out TermStore termStore, out TermSet termSet);

    }
}
