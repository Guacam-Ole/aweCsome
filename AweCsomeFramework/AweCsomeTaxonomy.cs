using E=AweCsome.Entities;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AweCsome.Interfaces;
using AweCsome.Enumerations;
using AweCsome.Entities;

namespace AweCsome
{
    // TODO: Cleanup, Log & Stuff

    public class AweCsomeTaxonomy : IAweCsomeTaxonomy
    {
        private ClientContext _clientContext;
        public int Lcid { get; set; } = 1033;

        public AweCsomeTaxonomy(ClientContext clientContext)
        {
            _clientContext = clientContext;
        }

        private AweCsomeTag GetTermChildren(Term term, AweCsomeTag parent)
        {
            var currentTag = new AweCsomeTag
            {
                Name = term.Name,
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
            return tag.Name != null && tag.Name.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        public void GetTermSetIds(TaxonomyTypes taxonomyType, string termSetName, string groupName, bool createIfNotExisting, out Guid termStoreId, out Guid termSetId)
        {
            TermStore termStore;
            TermSet termSet;
            Site site = _clientContext.Site;
            GetTermSet(taxonomyType, termSetName, groupName, createIfNotExisting, out termStore, out termSet);

            if (termSet == null)
            {
                if (!createIfNotExisting) throw new KeyNotFoundException("Taxonomy missing");

                TermGroup termGroup = groupName == null
                    ? termStore.GetSiteCollectionGroup(site, createIfNotExisting)
                    : termStore.GetTermGroupByName(groupName);

                termSetId = Guid.NewGuid();
                TermSet termSetColl = termGroup.CreateTermSet(termSetName, termSetId, Lcid);
                termSetColl.IsOpenForTermCreation = true;
                _clientContext.Load(termGroup.TermSets);
                _clientContext.ExecuteQuery();
                termSet = termGroup.TermSets.FirstOrDefault(ts => ts.Name == termSetName);
            }

            _clientContext.Load(termStore, ts => ts.Id);
            _clientContext.ExecuteQuery();

            termStoreId = termStore.Id;
            termSetId = termSet == null ? Guid.Empty : termSet.Id;
        }

        public void GetTermSet(TaxonomyTypes taxonomyType, string termSetName, string groupName, bool createIfMissing, out TermStore termStore, out TermSet termSet)
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
                case TaxonomyTypes.Keywords:
                    termStore = session.GetDefaultKeywordsTermStore();
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
                    TermGroup termGroup = groupName == null
                    ? termStore.GetSiteCollectionGroup(site, createIfMissing)
                    : termStore.GetTermGroupByName(groupName);
                    System.Threading.Thread.Sleep(1000);
                    if (termGroup == null || termGroup.TermSets == null) return;

                    _clientContext.Load(termGroup);
                    _clientContext.Load(termGroup.TermSets);
                    _clientContext.ExecuteQuery();
                    System.Threading.Thread.Sleep(1000);
                    termSet = termGroup.TermSets.FirstOrDefault(ts => ts.Name == termSetName);
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
                Name = termSet.Name,
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
        private  Term GetTermById(TaxonomyTypes taxonomyType, string termSetName, string groupName, Guid id)
        {
            GetTermSet(taxonomyType, termSetName, groupName, false, out TermStore termStore, out TermSet termSet);
            var term=termSet.GetAllTerms().GetById(id);
            return term;
        }

        public Guid AddTerm(TaxonomyTypes taxonomyType, string termSetName, string groupName, Guid? parentId, string name)
        {
            GetTermSet(taxonomyType, termSetName, groupName, false, out TermStore termStore, out TermSet termSet);
            Guid id = Guid.NewGuid();
            if (parentId == null)
            {
                termSet.CreateTerm(name, Lcid, id);
            } else
            {
                var parentTerm = termSet.GetAllTerms().GetById(parentId.Value);
                _clientContext.Load(parentTerm);
                _clientContext.ExecuteQuery();
                parentTerm.CreateTerm(name, Lcid, id);
            }
            _clientContext.ExecuteQuery();
            return id;
        }

        public void RenameTerm(TaxonomyTypes taxonomyType, string termSetName, string groupName, Guid id, string name)
        {
            var term = GetTermById(taxonomyType, termSetName, groupName, id);
            term.Name = name;
            _clientContext.ExecuteQuery();
        }

        public void DeleteTerm(TaxonomyTypes taxonomyType, string termSetName, string groupName, Guid id)
        {
            var term = GetTermById(taxonomyType, termSetName, groupName, id);
            term.DeleteObject();
            _clientContext.ExecuteQuery();
        }
    }
}
