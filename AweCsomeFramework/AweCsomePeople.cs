using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using log4net;
using Microsoft.SharePoint.ApplicationPages.ClientPickerQuery;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;
using AweCsome.Interfaces;

namespace AweCsome
{
    public class AweCsomePeople : IAweCsomePeople
    {
        private ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ClientContext _clientContext;

        public ClientContext ClientContext { set { _clientContext = value; } }

        public List<User> Search(string query, string uniqueField, int maxSuggestions = 100, PrincipalSource principalSource = PrincipalSource.All, PrincipalType principalType = PrincipalType.User, int sharePointGroupId = -1)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;

            ClientPeoplePickerQueryParameters querryParams = new ClientPeoplePickerQueryParameters();
            querryParams.AllowMultipleEntities = false;
            querryParams.MaximumEntitySuggestions = maxSuggestions;
            querryParams.PrincipalSource = principalSource;
            querryParams.PrincipalType = principalType;
            querryParams.QueryString = query;
            querryParams.SharePointGroupID = sharePointGroupId;

            //execute query to Sharepoint
            ClientResult<string> clientResult = ClientPeoplePickerWebServiceInterface.ClientPeoplePickerSearchUser(_clientContext, querryParams);
            _clientContext.ExecuteQuery();
            dynamic target = new JavaScriptSerializer().DeserializeObject(clientResult.Value);
            var  matches = new List<User>();
            foreach (var user in target)
            {
                User ensuredUser = _clientContext.Web.EnsureUser(user[uniqueField]);
                _clientContext.Load(ensuredUser);
                matches.Add(ensuredUser);
            }
            _clientContext.ExecuteQuery();
            return matches;
        }
    }
}
