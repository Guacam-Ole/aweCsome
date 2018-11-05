using log4net;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome
{
    public class AweCsomeUser : IAweCsomeUser
    {
        private ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ClientContext _clientContext;

        public ClientContext ClientContext { set { _clientContext = value; } }

        private Web GetWeb()
        {
            return _clientContext.Web;
        }

        private User GetUserByIdFromWeb(int? userId, Web web, bool getGroups)
        {
            User user = null;
            user = userId == null ? web.CurrentUser : web.GetUserById(userId.Value);

            _clientContext.Load(user);
            if (getGroups) _clientContext.Load(user, usr => usr.Groups);
            _clientContext.ExecuteQuery();
            return user;
        }

        public bool UserIsInGroup(string groupName, int? userId = null)
        {
            return GetUserByIdFromWeb(userId, GetWeb(), true).Groups.FirstOrDefault(q => q.Title == groupName) != null;
        }

        public bool UserIsInGroup(Group group, int? userId = null)
        {
            _clientContext.Load(group);
            _clientContext.ExecuteQuery();
            return UserIsInGroup(group.Title, userId);
        }

        public bool UserIsMember(int? userId = null)
        {
            return UserIsInGroup(GetWeb().AssociatedMemberGroup, userId);
        }

        public bool UserIsOwner(int? userId = null)
        {
            return UserIsInGroup(GetWeb().AssociatedOwnerGroup, userId);
        }

        public bool UserIsVisitor(int? userId = null)
        {
            return UserIsInGroup(GetWeb().AssociatedVisitorGroup, userId);
        }

        public User GetUser(int? userId = null, bool getGroups = false)
        {
            return GetUserByIdFromWeb(userId, GetWeb(), getGroups);
        }
    }
}
