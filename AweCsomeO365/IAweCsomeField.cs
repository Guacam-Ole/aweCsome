using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AweCsomeO365
{
    public interface IAweCsomeField
    {
        void AddFieldToList( List sharePointList, PropertyInfo property, Dictionary<string,Guid> lookupTableIds);
    }
}
