using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Attributes.TableAttributes
{
    public class UrlAttribute : Attribute
    {
        public string Url { get; set; }
        public UrlAttribute(string url)
        {
            Url = url;
        }
    }
}
