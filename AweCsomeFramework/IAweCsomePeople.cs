using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;

namespace AweCsome
{
    public interface IAweCsomePeople
    {
        List<User> Search(string query, string uniqueField, int maxSuggestions=100, PrincipalSource principalSource= PrincipalSource.All, PrincipalType prinzipalType= PrincipalType.User, int sharePointGroupId=-1);
    }
}
