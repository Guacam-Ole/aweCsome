using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome
{
    interface IAweCsomeUser
    {
        bool UserIsInGroup(string groupName, int? userId = null);
        bool UserIsMember(int? userId = null);
        bool UserIsOwner(int? userId = null);
        bool UserIsVisitor(int? userId = null);
        User GetUser(int? userId = null, bool getGroups=false);
    }
}
