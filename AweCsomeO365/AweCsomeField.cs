using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;

namespace AweCsomeO365
{
    public class AweCsomeField : IAweCsomeField
    {
        public void AddFieldToList(ClientContext clientContext, List sharePointList, PropertyInfo property)
        {
            throw new NotImplementedException();
        }
    }
}
