using System;
using AweCsome.Enumerations;

namespace AweCsome.Attributes.TableAttributes
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
