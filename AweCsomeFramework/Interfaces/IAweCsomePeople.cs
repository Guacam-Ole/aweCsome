using System.Collections.Generic;
using AweCsome.Entities;
using AweCsome.Enumerations;

namespace AweCsome.Interfaces
{
    public interface IAweCsomePeople
    {
        List<AweCsomeUser> Search(string query, string uniqueField, int maxSuggestions = 100, PrincipalSource principalSource = PrincipalSource.All, PrincipalType principalType = PrincipalType.User, int sharePointGroupId = -1);
        AweCsomeUser GetSiteUserById(int id);
        List<AweCsomeUser> GetUsersFromSiteGroup(string groupname);
        AweCsomeGroup GetGroupFromSite(string groupname);
        bool UserIsInGroup(string groupname, int? userId=null);
        AweCsomeUser GetCurrentUser();
    }
}
