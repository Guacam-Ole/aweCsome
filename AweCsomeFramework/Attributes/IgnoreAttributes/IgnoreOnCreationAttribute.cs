using System;

namespace AweCsome.Attributes.IgnoreAttributes
{
    public class IgnoreOnCreationAttribute : Attribute
    {

        public bool IgnoreOnCreation { get; set; } = true;
    }
}
