using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Enumerations
{
    public enum PrincipalSource
    {
        None = 0,
        UserInfoList = 1,
        Windows = 2,
        MembershipProvider = 4,
        RoleProvider = 8,
        All = 15
    }
    public enum PrincipalType
    {
        None = 0,
        User = 1,
        DistributionList = 2,
        SecurityGroup = 4,
        SharePointGroup = 8,
        All = 15
    }
}
