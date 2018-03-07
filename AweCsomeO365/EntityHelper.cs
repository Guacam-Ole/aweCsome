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

        private static void RemoveSuffixFromName(ref string name, string suffix)
        {
            if (name == null) return;
            if (name.EndsWith(suffix)) name = name.Substring(0, name.Length - suffix.Length);
        }

        public static void RemoveLookupIdFromFieldName(bool isArray, ref string internalName, ref string displayName)
        {
            RemoveSuffixFromName(ref internalName, isArray ? AweCsomeField.SuffixIds : AweCsomeField.SuffixId);
            RemoveSuffixFromName(ref displayName, isArray ? AweCsomeField.SuffixIds : AweCsomeField.SuffixId);
        }


        public static string GetInternalNameFromProperty(PropertyInfo propertyInfo)
        {
            Type propertyType = propertyInfo.PropertyType;
            var internalNameAttribute = propertyType.GetCustomAttribute<InternalNameAttribute>();
            string internalName = internalNameAttribute == null ? propertyInfo.Name : internalNameAttribute.InternalName;
            string displayName = null;
            if (AweCsomeField.PropertyIsLookup(propertyInfo)) RemoveLookupIdFromFieldName(propertyType.IsArray, ref internalName, ref displayName);
            return internalName;
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

        public static FieldLookupValue CreateLookupFromId(int id)
        {
            return new FieldLookupValue { LookupId = id };
        }

        public static FieldLookupValue[] CreateLookupsFromIds(int[] ids)
        {
            return ids.Select(id => new FieldLookupValue { LookupId = id }).ToArray();
        }

        public static object GetPropertyValueForItem<T>(PropertyInfo property, T entity)
        {
            Type propertyType = property.PropertyType;
            if (AweCsomeField.PropertyIsLookup(property))
            {
                if (propertyType == typeof(KeyValuePair<int, string>)) return CreateLookupFromId(((KeyValuePair<int, string>)property.GetValue(entity)).Key);
                if (propertyType == typeof(Dictionary<int, string>)) return CreateLookupsFromIds(((Dictionary<int, string>)property.GetValue(entity)).Select(q => q.Key).ToArray());
                if (propertyType.IsArray && propertyType.GetElementType().GetProperty(AweCsomeField.SuffixId) != null)
                {
                    List<int> ids = new List<int>();
                    foreach (var item in (object[])property.GetValue(entity))
                    {
                        ids.Add((int)item.GetType().GetProperty(AweCsomeField.SuffixId).GetValue(item));
                    }
                    return CreateLookupsFromIds(ids.ToArray());
                }
                if (propertyType.GetProperty(AweCsomeField.SuffixId) != null)
                {
                    var item = property.GetValue(entity);
                    return CreateLookupFromId(((int)item.GetType().GetProperty(AweCsomeField.SuffixId).GetValue(item)));
                }
            }
            if (propertyType.IsEnum)
            {
                return Enum.GetName(property.PropertyType, property.GetValue(entity));
            }
            return property.GetValue(entity);
        }
    }
}
