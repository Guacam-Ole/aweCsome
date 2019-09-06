using System;

namespace AweCsome.Attributes.IgnoreAttributes
{
    public class IgnoreOnUpdateAttribute : Attribute
    {
        public bool OnlyIfEmpty { get; set; } = false;
        public bool IgnoreOnUpdate { get; set; } = true;
    }
}
