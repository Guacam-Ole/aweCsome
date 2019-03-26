using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AweCsome.Interfaces;

namespace AweCsome
{
    public class AweCsomeHelpers : IAweCsomeHelpers
    {
        public string GetListName<T>()
        {
            return EntityHelper.GetInternalNameFromEntityType(typeof(T));
        }
    }
}
