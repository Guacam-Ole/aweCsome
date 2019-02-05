using AweCsome.Entities;
using Microsoft.SharePoint.Client.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome
{
    public enum TaxonomyTypes
    {
        SiteCollection
        // TODO: Other Locations as well
    }

    public interface IAweCsomeTaxonomy
    {
        void GetTermsetIds(TaxonomyTypes taxonomyLocatiom, string termsetName, string groupName, bool createIfNotExisting, out Guid termStoreId, out Guid termSetId);
        void GetTermSet(TaxonomyTypes taxonomyLocatiom, string termsetName, string groupName,  bool createIfMissing, out TermStore termStore, out TermSet termSet);
        AweCsomeTag Search(TaxonomyTypes taxonomyLocatiom, string termSetName, string groupName, string query);
    }
}
