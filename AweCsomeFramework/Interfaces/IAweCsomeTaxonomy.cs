using AweCsome.Entities;
//using Microsoft.SharePoint.Client.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Interfaces
{
    public enum TaxonomyTypes
    {
        SiteCollection,
        Keywords
    }

    public interface IAweCsomeTaxonomy
    {
        void GetTermSetIds(TaxonomyTypes taxonomyType, string termSetName, string groupName, bool createIfNotExisting, out Guid termStoreId, out Guid termSetId);
        //void GetTermSet(TaxonomyTypes taxonomyType, string termSetName, string groupName,  bool createIfMissing, out TermStore termStore, out TermSet termSet);
        AweCsomeTag Search(TaxonomyTypes taxonomyType, string termSetName, string groupName, string query);
        Guid AddTerm(TaxonomyTypes taxonomyType, string termSetName, string groupName, Guid? parentId, string name);
        void RenameTerm(TaxonomyTypes taxonomyType, string termSetName, string groupName, Guid id, string name);
        void DeleteTerm(TaxonomyTypes taxonomyType, string termSetName, string groupName, Guid id);
    }
}
