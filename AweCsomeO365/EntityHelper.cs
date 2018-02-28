using AweCsomeO365.Attributes;
using AweCsomeO365.Attributes.FieldAttributes;
using AweCsomeO365.Attributes.TableAttributes;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365
{
    public static class EntityHelper
    {
        public static string GetInternalNameFromProperty(PropertyInfo propertyInfo)
        {
            Type propertyType = propertyInfo.PropertyType;
            var internalNameAttribute = propertyType.GetCustomAttribute<InternalNameAttribute>();
            return internalNameAttribute == null ? propertyInfo.Name : internalNameAttribute.InternalName;
        }

        public static string GetInternalNameFromEntityType(Type entityType)
        {
            var internalNameAttribute = entityType.GetCustomAttribute<InternalNameAttribute>();
            return internalNameAttribute == null ? entityType.Name : internalNameAttribute.InternalName;
        }

        public static string GetDisplayNameFromEntityType(PropertyInfo propertyInfo)
        {
            Type propertyType = propertyInfo.PropertyType;
            var displayNameAttribute = propertyType.GetCustomAttribute<DisplayNameAttribute>();
            return displayNameAttribute == null ? propertyInfo.Name : displayNameAttribute.DisplayName;
        }

        public static int GetListTemplateType(Type entityType)
        {
            var listTemplateTypeAttribute = entityType.GetCustomAttribute<ListTemplateTypeAttribute>();
            return listTemplateTypeAttribute == null ? (int)ListTemplateType.GenericList : listTemplateTypeAttribute.TemplateTypeId;
        }

        public static string GetDescriptionFromEntityType(Type entityType)
        {
            var descriptionAttribute = entityType.GetCustomAttribute<DescriptionAttribute>();
            return descriptionAttribute?.Description;
        }

        public static bool IsGenericList(this Type type)
        {
            return (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)));
        }

        public static bool IsDictionary(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);

        }


    }
}
