using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AweCsome
{
    public interface IAweCsomeField
    {
        void AddFieldToList( List sharePointList, PropertyInfo property, Dictionary<string,Guid> lookupTableIds);

        void ChangeDisplaynameFromField(List sharePointList, PropertyInfo property);

        Field GetFieldDefinition(List sharePointList, PropertyInfo property);
    }
}
