using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AweCsomeO365
{
    internal class SharePointEssentials
    {
        private SharePointContext GetSharePointContext()
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext.Current);
            if (spContext == null) throw new HttpException(419, "Auth expired");
            return spContext;
        }

        internal ClientContext GetClientContext()
        {
            switch (AweCsomeConfiguration.PermissionScope)
            {
                case AweCsomeConfiguration.PermissionScopes.App:
                    switch (AweCsomeConfiguration.TargetWeb)
                    {
                        case AweCsomeConfiguration.TargetWebs.AppWeb:
                            return GetSharePointContext().CreateAppOnlyClientContextForSPAppWeb();
                        case AweCsomeConfiguration.TargetWebs.HostWeb:
                            return GetSharePointContext().CreateAppOnlyClientContextForSPHost();
                    }
                    throw new Exception("Unexpected value");
                case AweCsomeConfiguration.PermissionScopes.User:
                    switch (AweCsomeConfiguration.TargetWeb)
                    {
                        case AweCsomeConfiguration.TargetWebs.AppWeb:
                            return GetSharePointContext().CreateUserClientContextForSPAppWeb();
                        case AweCsomeConfiguration.TargetWebs.HostWeb:
                            return GetSharePointContext().CreateUserClientContextForSPHost();
                    }
                    throw new Exception("Unexpected value");
            }
            throw new Exception("Unexpected value");
        }
    }
}
