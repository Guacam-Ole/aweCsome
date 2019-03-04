using System;
using AweCsome.Entities;
using AweCsome.Enumerations;

namespace AweCsome.Interfaces
{
    public interface IAweCsomeTaxonomy
    {
        void GetTermSetIds(TaxonomyTypes taxonomyType, string termSetName, string groupName, bool createIfNotExisting, out Guid termStoreId, out Guid termSetId);
        AweCsomeTag Search(TaxonomyTypes taxonomyType, string termSetName, string groupName, string query);
        Guid AddTerm(TaxonomyTypes taxonomyType, string termSetName, string groupName, Guid? parentId, string name);
        void RenameTerm(TaxonomyTypes taxonomyType, string termSetName, string groupName, Guid id, string name);
        void DeleteTerm(TaxonomyTypes taxonomyType, string termSetName, string groupName, Guid id);
    }
}
