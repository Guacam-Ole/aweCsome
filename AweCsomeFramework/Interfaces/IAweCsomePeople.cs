using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;

namespace AweCsome.Interfaces
{
    public interface IAweCsomePeople
    {
        List<User> Search(string query, string uniqueField, int maxSuggestions=100, int principalSource= 15, int principalType= 1, int sharePointGroupId=-1);
        User GetSiteUserById(int id);
        List<User> GetUsersFromSiteGroup(string groupname);
        Group GetGroupFromSite(string groupname);
    }
}
