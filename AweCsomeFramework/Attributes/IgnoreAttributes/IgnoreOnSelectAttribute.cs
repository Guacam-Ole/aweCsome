using System;

namespace AweCsome.Attributes.IgnoreAttributes
{
    public class IgnoreOnSelectAttribute : Attribute
    {
        public bool IgnoreOnSelect { get; set; } = true;
    }
}
