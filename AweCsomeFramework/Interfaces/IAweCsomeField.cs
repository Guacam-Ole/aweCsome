using System;
using System.Collections.Generic;
using System.Reflection;

namespace AweCsome.Interfaces
{
    public interface IAweCsomeField
    {
        object AddFieldToList(object sharePointList, PropertyInfo property, Dictionary<string, Guid> lookupTableIds);

        void ChangeDisplaynameFromField(object sharePointList, PropertyInfo property);

        void ChangeTypeFromField(object sharePointList, PropertyInfo property);

        object GetFieldDefinition(object sharePointList, PropertyInfo property);

        bool IsMulti(Type propertyType);
    }
}
