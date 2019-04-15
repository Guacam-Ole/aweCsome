using System.Collections.Generic;
using AweCsome.Enumerations;

namespace AweCsome.Interfaces
{
    public interface IAweCsomePeople
    {
        List<object> Search(string query, string uniqueField, int maxSuggestions = 100, PrincipalSource principalSource = PrincipalSource.All, PrincipalType principalType = PrincipalType.User, int sharePointGroupId = -1);
        object GetSiteUserById(int id);
        List<object> GetUsersFromSiteGroup(string groupname);
        object GetGroupFromSite(string groupname);
        bool UserIsInGroup(string groupname, int? userId=null);
        object GetCurrentUser();
    }
}
