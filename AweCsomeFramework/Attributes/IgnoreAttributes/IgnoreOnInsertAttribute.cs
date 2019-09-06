using System;

namespace AweCsome.Attributes.IgnoreAttributes
{
    public class IgnoreOnInsertAttribute : Attribute
    {
        public bool OnlyIfEmpty { get; set; } = false;
        public bool IgnoreOnInsert { get; set; } = true;
    }
}
