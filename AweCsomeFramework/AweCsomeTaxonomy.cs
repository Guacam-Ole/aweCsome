using AweCsome.Entities;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AweCsome.Interfaces;

namespace AweCsome
{
    // TODO: Cleanup, Log & Stuff

    public class AweCsomeTaxonomy : IAweCsomeTaxonomy
    {
        private ClientContext _clientContext;
        public ClientContext ClientContext { set { _clientContext = value; } }
        public int Lcid { get; set; } = 1033;

        //public void GetTermsetIds(string termsetName, bool createIfNotExisting, out Guid termStoreId, out Guid termSetId)
        //{
        //    int lcid = 1033;
        //    TermStore termStore;
        //    TermSet termSet;
        //    Site site = _clientContext.Site;

        //    GetTermSetFromSiteCollection(termsetName, lcid, createIfNotExisting, out termStore, out termSet);

        //    if (termSet == null)
        //    {
        //        if (!createIfNotExisting) throw new KeyNotFoundException("Taxonomy missing");

        //        TermGroup termGroup = termStore.GetSiteCollectionGroup(site, true);
        //        termSetId = Guid.NewGuid();
        //        TermSet termSetColl = termGroup.CreateTermSet(termsetName, termSetId, lcid);
        //        termSetColl.IsOpenForTermCreation = true;
        //        _clientContext.ExecuteQuery();
        //        _clientContext.Load(termGroup.TermSets);
        //        _clientContext.ExecuteQuery();
        //        termSet = termGroup.TermSets.FirstOrDefault(ts => ts.Name == termsetName);
        //    }

        //    _clientContext.Load(termStore, ts => ts.Id);
        //    _clientContext.ExecuteQuery();

        //    termStoreId = termStore.Id;
        //    termSetId = termSet == null ? Guid.Empty : termSet.Id;
        //}

        public void GetTermSetFromSiteCollection(string termsetName, int lcid, bool createIfMissing, out TermStore siteCollectionTermstore, out TermSet termSet)
        {
            termSet = null;
            Site site = _clientContext.Site;

            TaxonomySession session = TaxonomySession.GetTaxonomySession(_clientContext);
            siteCollectionTermstore = session.GetDefaultSiteCollectionTermStore();

            try
            {
                if (siteCollectionTermstore != null)
                {
                    _clientContext.Load(siteCollectionTermstore);
                    _clientContext.ExecuteQuery();
                    System.Threading.Thread.Sleep(1000);
                    TermGroup termGroup = siteCollectionTermstore.GetSiteCollectionGroup(site, createIfMissing);
                    System.Threading.Thread.Sleep(1000);
                    if (termGroup == null || termGroup.TermSets == null) return;

                    _clientContext.Load(termGroup);
                    _clientContext.Load(termGroup.TermSets);
                    _clientContext.ExecuteQuery();
                    System.Threading.Thread.Sleep(1000);
                    termSet = termGroup.TermSets.FirstOrDefault(ts => ts.Name == termsetName);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private AweCsomeTag GetTermChildren(Term term, AweCsomeTag parent)
        {
            var currentTag = new AweCsomeTag
            {
                Title = term.Name,
                Id = term.Id,
                ParentId = parent.Id,
                Children = new List<AweCsomeTag>()
            };
            if (term.TermsCount > 0)
            {
                _clientContext.Load(term.Terms);
                _clientContext.ExecuteQuery();
                foreach (var child in term.Terms)
                {
                    currentTag.Children.Add(GetTermChildren(child, currentTag));
                }
            }
            return currentTag;
        }

        private bool SearchInsideTaxonomy(AweCsomeTag tag, string query)
        {
            for (int i = tag.Children.Count - 1; i >= 0; i--)
            {
                if (!SearchInsideTaxonomy(tag.Children[i], query))
                {
                    tag.Children.RemoveAt(i);
                }
            }
            if (tag.Children.Count > 0)
            {
                return true;
            }
            return tag.Title != null && tag.Title.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        public void GetTermsetIds(TaxonomyTypes taxonomyType, string termsetName, string groupname, bool createIfMissing, out Guid termStoreId, out Guid termSetId)
        {
            TermStore termStore;
            TermSet termSet;
            Site site = _clientContext.Site;

            switch (taxonomyType)
            {
                case TaxonomyTypes.SiteCollection:
                    GetTermSet(taxonomyType, termsetName, groupname,  createIfMissing, out termStore, out termSet);
                    break;
                default:
                    throw new Exception("Unknown Taxomylocation");
            }

            if (termSet == null)
            {
                if (!createIfMissing) throw new KeyNotFoundException("Taxonomy missing");

                TermGroup termGroup = groupname == null
                    ? termStore.GetSiteCollectionGroup(site, createIfMissing)
                    : termStore.GetTermGroupByName(groupname);

                termSetId = Guid.NewGuid();
                TermSet termSetColl = termGroup.CreateTermSet(termsetName, termSetId, Lcid);
                termSetColl.IsOpenForTermCreation = true;
                _clientContext.Load(termGroup.TermSets);
                _clientContext.ExecuteQuery();
                termSet = termGroup.TermSets.FirstOrDefault(ts => ts.Name == termsetName);
            }

            _clientContext.Load(termStore, ts => ts.Id);
            _clientContext.ExecuteQuery();

            termStoreId = termStore.Id;
            termSetId = termSet == null ? Guid.Empty : termSet.Id;
        }


        public void GetTermSet(TaxonomyTypes taxonomyType, string termsetName, string groupname, bool createIfMissing, out TermStore termStore, out TermSet termSet)
        {
            termSet = null;
            Site site = _clientContext.Site;
            termStore = null;

            TaxonomySession session = TaxonomySession.GetTaxonomySession(_clientContext);
            switch (taxonomyType)
            {
                case TaxonomyTypes.SiteCollection:
                    termStore = session.GetDefaultSiteCollectionTermStore();
                    break;
                default:
                    throw new Exception("Unexpected Taxonomytype");
            }
            

            try
            {
                if (termStore != null)
                {
                    _clientContext.Load(termStore);
                    _clientContext.ExecuteQuery();
                    System.Threading.Thread.Sleep(1000);
                    TermGroup termGroup = groupname == null
                    ? termStore.GetSiteCollectionGroup(site, createIfMissing)
                    : termStore.GetTermGroupByName(groupname);
                    System.Threading.Thread.Sleep(1000);
                    if (termGroup == null || termGroup.TermSets == null) return;

                    _clientContext.Load(termGroup);
                    _clientContext.Load(termGroup.TermSets);
                    _clientContext.ExecuteQuery();
                    System.Threading.Thread.Sleep(1000);
                    termSet = termGroup.TermSets.FirstOrDefault(ts => ts.Name == termsetName);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public AweCsomeTag Search(TaxonomyTypes taxonomyType, string termSetName, string groupName, string query)
        {
            GetTermSet(taxonomyType, termSetName, groupName, false, out TermStore termStore, out TermSet termSet);
            TermCollection allTerms = termSet.Terms;
            _clientContext.Load(termSet, q => q.Name);
            _clientContext.Load(allTerms);
            _clientContext.ExecuteQuery();
            var rootTag = new AweCsomeTag
            {
                Children = new List<AweCsomeTag>(),
                Title = termSet.Name,
                Id = termSet.Id,
                TermStoreName = termStore.Name
            };

            foreach (var term in allTerms)
            {
                rootTag.Children.Add(GetTermChildren(term, rootTag));
            }
            if (query != null) SearchInsideTaxonomy(rootTag, query);

            return rootTag;
        }

        public Guid AddTerm(TaxonomyTypes taxonomyType, string termSetName, string groupName, Guid? parentId, string name)
        {
            throw new NotImplementedException();
        }

        public void RenameTerm(TaxonomyTypes taxonomyType, string termSetName, string groupName, Guid id, string name)
        {
            throw new NotImplementedException();
        }

        public void DeleteTerm(TaxonomyTypes taxonomyType, string termSetName, string groupName, Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
