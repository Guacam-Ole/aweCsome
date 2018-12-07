using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome
{
    // TODO: Cleanup, Log & Stuff

    public class AweCsomeTaxonomy : IAweCsomeTaxonomy
    {
        private ClientContext _clientContext;
        public ClientContext ClientContext { set { _clientContext = value; } }

        public void GetTaxonomyFieldInfo(string termsetName, bool createIfNotExisting, out Guid termStoreId, out Guid termSetId)
        {
            int lcid = 1033;
            TermStore termStore;
            TermSet termSet;
            Site site = _clientContext.Site;

            GetTermSet(termsetName, lcid, createIfNotExisting, out termStore, out termSet);

            if (termSet == null)
            {
                if (!createIfNotExisting) throw new KeyNotFoundException("Taxonomy missing");

                TermGroup termGroup = termStore.GetSiteCollectionGroup(site, true);
                termSetId = Guid.NewGuid();
                TermSet termSetColl = termGroup.CreateTermSet(termsetName, termSetId, lcid);
                termSetColl.IsOpenForTermCreation = true;
                _clientContext.ExecuteQuery();
                _clientContext.Load(termGroup.TermSets);
                _clientContext.ExecuteQuery();
                termSet = termGroup.TermSets.FirstOrDefault(ts => ts.Name == termsetName);
            }

            _clientContext.Load(termStore, ts => ts.Id);
            _clientContext.ExecuteQuery();

            termStoreId = termStore.Id;
            termSetId = termSet == null ? Guid.Empty : termSet.Id;
        }

        public void GetTermSet(string termsetName, int lcid, bool createIfMissing, out TermStore termStore, out TermSet termSet)
        {

            termSet = null;
            Site site = _clientContext.Site;

            TaxonomySession session = TaxonomySession.GetTaxonomySession(_clientContext);
            termStore = session.GetDefaultSiteCollectionTermStore();

            try
            {
                if (termStore != null)
                {
                    _clientContext.Load(termStore);
                    _clientContext.ExecuteQuery();
                    System.Threading.Thread.Sleep(1000);
                    TermGroup termGroup = termStore.GetSiteCollectionGroup(site, createIfMissing);
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
    }
}
