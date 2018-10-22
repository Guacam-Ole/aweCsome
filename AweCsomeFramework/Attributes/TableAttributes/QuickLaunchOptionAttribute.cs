using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes.TableAttributes
{
    public class QuickLaunchOptionAttribute : Attribute
    {
        public QuickLaunchOptions QuickLaunchOption { get; set; }

        public QuickLaunchOptionAttribute(QuickLaunchOptions quickLaunchOption)
        {
            QuickLaunchOption = quickLaunchOption;
        }

        public QuickLaunchOptionAttribute(bool displayOnQuickLaunch)
        {
            QuickLaunchOption = displayOnQuickLaunch ? QuickLaunchOptions.On : QuickLaunchOptions.Off;
        }
    }
}
