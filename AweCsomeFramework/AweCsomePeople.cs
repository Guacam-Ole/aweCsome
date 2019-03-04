using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using E = AweCsome.Enumerations;
using AweCsome.Interfaces;
using log4net;
using Microsoft.SharePoint.ApplicationPages.ClientPickerQuery;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;

namespace AweCsome
{
    public class AweCsomePeople : IAweCsomePeople
    {
        private ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ClientContext _clientContext;

        public AweCsomePeople(ClientContext clientContext)
        {
            _clientContext = clientContext;
        }

        public User GetSiteUserById(int id)
        {
            var user = _clientContext.Web.SiteUsers.FirstOrDefault(q => q.Id == id);
            _clientContext.Load(user);
            _clientContext.ExecuteQuery();
            return user;
        }

        public List<User> GetUsersFromSiteGroup(string groupname)
        {
            var group = GetGroupFromSite(groupname);
            if (group == null) return null;
            var users = group.Users;
            _clientContext.Load(users);
            _clientContext.ExecuteQuery();
            return users.ToList();
        }

        public Group GetGroupFromSite(string groupname)
        {
            if (!_clientContext.Web.GroupExists(groupname)) return null;
            var group = _clientContext.Web.SiteGroups.FirstOrDefault(q => q.Title == groupname);
            _clientContext.Load(group);
            _clientContext.ExecuteQuery();
            return group;
        }

        public List<object> Search(string query, string uniqueField, int maxSuggestions = 100, Enumerations.PrincipalSource principalSource = Enumerations.PrincipalSource.All, Enumerations.PrincipalType principalType = Enumerations.PrincipalType.User, int sharePointGroupId = -1)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;

            ClientPeoplePickerQueryParameters querryParams = new ClientPeoplePickerQueryParameters();
            querryParams.AllowMultipleEntities = false;
            querryParams.MaximumEntitySuggestions = maxSuggestions;
            querryParams.PrincipalSource = (PrincipalSource)principalSource;
            querryParams.PrincipalType = (PrincipalType)principalType;
            querryParams.QueryString = query;
            querryParams.SharePointGroupID = sharePointGroupId;

            //execute query to Sharepoint
            ClientResult<string> clientResult = ClientPeoplePickerWebServiceInterface.ClientPeoplePickerSearchUser(_clientContext, querryParams);
            _clientContext.ExecuteQuery();
            dynamic target = new JavaScriptSerializer().DeserializeObject(clientResult.Value);
            var matches = new List<object>();
            foreach (var user in target)
            {
                User ensuredUser = _clientContext.Web.EnsureUser(user[uniqueField]);
                _clientContext.Load(ensuredUser);
                matches.Add(ensuredUser);
            }
            _clientContext.ExecuteQuery();
            return matches;
        }

        List<object> IAweCsomePeople.Search(string query, string uniqueField, int maxSuggestions, E.PrincipalSource principalSource, E.PrincipalType principalType, int sharePointGroupId)
        {
            throw new NotImplementedException();
        }

        object IAweCsomePeople.GetSiteUserById(int id)
        {
            throw new NotImplementedException();
        }

        List<object> IAweCsomePeople.GetUsersFromSiteGroup(string groupname)
        {
            throw new NotImplementedException();
        }

        object IAweCsomePeople.GetGroupFromSite(string groupname)
        {
            throw new NotImplementedException();
        }
    }
}
