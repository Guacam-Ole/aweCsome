using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365
{
    public static class AweCsomeConfiguration
    {
        public enum TargetWebs { HostWeb, AppWeb }
        public enum PermissionScopes { User, App }

        public static TargetWebs TargetWeb { get; set; }
        public static PermissionScopes PermissionScope { get; set; }
    }
}
