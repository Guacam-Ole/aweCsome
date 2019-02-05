using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Attributes.TableAttributes
{
    public class TableUrlAttribute : Attribute
    {
        public string Url { get; set; }
        public TableUrlAttribute(string url)
        {
            Url = url;
        }
    }
}
