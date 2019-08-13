using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using AweCsome.Entities;
using AweCsome.Interfaces;
using log4net;
using Microsoft.SharePoint.ApplicationPages.ClientPickerQuery;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;
using E = AweCsome.Enumerations;

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

        private User GetSiteUserById(int id)
        {
            var user = _clientContext.Web.GetUserById(id);
            _clientContext.Load(user);
            _clientContext.ExecuteQuery();
            return user;
        }

        private List<AweCsomeUser> GetUsersFromSiteGroup(string groupname)
        {
            var group = GetSharePointGroupFromSite(groupname);
            if (group == null) return null;
            var users = group.Users;
            _clientContext.Load(users);
            _clientContext.ExecuteQuery();
            return users.ToList().Select(q => ToAweCsomeUser(q)).ToList();
        }

        public  AweCsomeGroup GetGroupFromSite(string groupname)
        {
            return ToAweCsomeGroup(GetSharePointGroupFromSite(groupname));
        }

        private Group GetSharePointGroupFromSite(string groupname)
        {
            var allGroups = _clientContext.Web.SiteGroups;
            _clientContext.Load(allGroups);
            _clientContext.ExecuteQuery();
            var group = allGroups.FirstOrDefault(q => q.Title == groupname);
            if (group == null) return null;

            _clientContext.Load(group);
            _clientContext.ExecuteQuery();
            return group;
        }

        private List<AweCsomeUser> Search(string query, string uniqueField, int maxSuggestions = 100, Enumerations.PrincipalSource principalSource = Enumerations.PrincipalSource.All, Enumerations.PrincipalType principalType = Enumerations.PrincipalType.User, int sharePointGroupId = -1)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;

            ClientPeoplePickerQueryParameters querryParams = new ClientPeoplePickerQueryParameters();
            querryParams.AllowMultipleEntities = false;
            querryParams.MaximumEntitySuggestions = maxSuggestions;
            querryParams.PrincipalSource = (PrincipalSource)principalSource;
            querryParams.PrincipalType = (PrincipalType)principalType;
            querryParams.QueryString = query;
            querryParams.SharePointGroupID = sharePointGroupId;

            ClientResult<string> clientResult = ClientPeoplePickerWebServiceInterface.ClientPeoplePickerSearchUser(_clientContext, querryParams);
            _clientContext.ExecuteQuery();
            dynamic target = new JavaScriptSerializer().DeserializeObject(clientResult.Value);
            var matches = new List<AweCsomeUser>();
            foreach (var user in target)
            {
                User ensuredUser = _clientContext.Web.EnsureUser(user[uniqueField]);
                _clientContext.Load(ensuredUser);
                _clientContext.ExecuteQuery();
                matches.Add(ToAweCsomeUser(ensuredUser, false));
            }
            _clientContext.ExecuteQuery();
            return matches;
        }

        List<AweCsomeUser> IAweCsomePeople.Search(string query, string uniqueField, int maxSuggestions, E.PrincipalSource principalSource, E.PrincipalType principalType, int sharePointGroupId)
        {
            return Search(query, uniqueField, maxSuggestions, principalSource, principalType, sharePointGroupId);
        }

        AweCsomeUser IAweCsomePeople.GetSiteUserById(int id)
        {
            return ToAweCsomeUser(GetSiteUserById(id));
        }

        List<AweCsomeUser> IAweCsomePeople.GetUsersFromSiteGroup(string groupname)
        {
            return GetUsersFromSiteGroup(groupname);
        }

        AweCsomeGroup IAweCsomePeople.GetGroupFromSite(string groupname)
        {
            return GetGroupFromSite(groupname);
        }

        public bool UserIsInGroup(string groupname, int? userId = null)
        {
            try
            {
                userId = userId ?? GetCurrentUser().Id;
                return GetUsersFromSiteGroup(groupname)?.FirstOrDefault(q => q.Id == userId) != null;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public AweCsomeUser GetCurrentUser()
        {
            var web = _clientContext.Web;
            _clientContext.Load(web, w => w.CurrentUser);
            _clientContext.ExecuteQuery();
            return ToAweCsomeUser(web.CurrentUser);
        }

        private AweCsomeUser ToAweCsomeUser(User user, bool getGroups=true)
        {
            var aweCsomeUser = new AweCsomeUser { Groups = new List<AweCsomeGroup>() };
            var userType = typeof(User);
            foreach (var property in typeof(AweCsomeUser).GetProperties())
            {
                if (!property.CanWrite) continue;
                var userProperty = userType.GetProperty(property.Name);
                if (userProperty == null) continue;
                if (!userProperty.CanRead) continue;
                if (userProperty.PropertyType != property.PropertyType) continue;
                property.SetValue(aweCsomeUser, userProperty.GetValue(user));
            }

            if (getGroups && user.Groups!=null) 
            {
                foreach( var group in user.Groups)
                {
                    aweCsomeUser.Groups.Add(ToAweCsomeGroup(group, false));
                }
            }
            return aweCsomeUser;
        }

        private AweCsomeGroup ToAweCsomeGroup(Group group, bool getUsers=true)
        {
            var aweCsomeGroup = new AweCsomeGroup { Users = new List<AweCsomeUser>()};
            var groupType = typeof(Group);
            foreach (var property in typeof(AweCsomeGroup).GetProperties())
            {
                if (!property.CanWrite) continue;
                var groupProperty = groupType.GetProperty(property.Name);
                if (groupProperty == null) continue;
                if (!groupProperty.CanRead) continue;
                if (groupProperty.PropertyType != property.PropertyType) continue;
                property.SetValue(aweCsomeGroup, groupProperty.GetValue(property.Name));
            }

            if (getUsers && group.Users!= null)
            {
                foreach (var user in group.Users)
                {
                    aweCsomeGroup.Users.Add(ToAweCsomeUser(user, false));
                }
            }
            return aweCsomeGroup;
        }
    }
}
