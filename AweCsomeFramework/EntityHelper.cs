
using AweCsome.Attributes;
using AweCsome.Attributes.FieldAttributes;
using AweCsome.Attributes.TableAttributes;
using log4net;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome
{
    public static class EntityHelper
    {
        private static ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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
            var internalNameAttribute = propertyInfo.GetCustomAttribute<InternalNameAttribute>();
            string internalName = internalNameAttribute == null ? propertyInfo.Name : internalNameAttribute.InternalName;
            string displayName = null;
            if (PropertyIsLookup(propertyInfo)) RemoveLookupIdFromFieldName(propertyType.IsArray, ref internalName, ref displayName);
            return internalName;
        }

        public static string GetInternalNameFromEntityType(Type entityType)
        {
            var internalNameAttribute = entityType.GetCustomAttribute<InternalNameAttribute>();
            return internalNameAttribute == null ? entityType.Name : internalNameAttribute.InternalName;
        }

    


        public static string GetDisplayNameFromEntityType(Type entityType)
        {
            var displayNameAttribute = entityType.GetCustomAttribute<DisplayNameAttribute>();
            return displayNameAttribute == null ? entityType.Name : displayNameAttribute.DisplayName;
        }

        public static string GetDisplayNameFromProperty(PropertyInfo propertyInfo)
        {
            Type propertyType = propertyInfo.PropertyType;
            var displayNameAttribute = propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
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
            if (ids == null) return null;
            return ids.Select(id => new FieldLookupValue { LookupId = id }).ToArray();
        }

        public static FieldUserValue CreateUserFromId(int id)
        {
            return new FieldUserValue { LookupId = id };
        }

        public static FieldUserValue[] CreateUsersFromIds(int[] ids)
        {
            if (ids == null) return null;
            return ids.Select(id => new FieldUserValue { LookupId = id }).ToArray();
        }

        public static bool PropertyIsUser(PropertyInfo property)
        {
            return (property.GetCustomAttribute<UserAttribute>(true) != null);
        }


        public static bool PropertyIsUrl(PropertyInfo property)
        {
            return property.GetCustomAttribute<UrlAttribute>() != null;
        }

        public static bool PropertyIsLookup(PropertyInfo property)
        {
            if (property.GetCustomAttribute<LookupBaseAttribute>(true) != null) return true;
            if (property.GetCustomAttribute<UserAttribute>(true) != null) return true;
            Type propertyType = property.PropertyType;
            if (propertyType == typeof(KeyValuePair<int, string>)) return true; // Single-Lookup
            if (propertyType == typeof(Dictionary<int, string>)) return true; // Multi-Lookup
            if (propertyType.GetProperty(AweCsomeField.SuffixId) != null) return true; // Single Lookup with complex type
            if (propertyType.IsArray && propertyType.GetElementType().GetProperty(AweCsomeField.SuffixId) != null) return true; // Multi Lookup with complex type
            return false;
        }

        public static bool PropertyIsTaxonomy(PropertyInfo property)
        {
            return (property.GetCustomAttribute<ManagedMetadataAttribute>(true) != null);
        }

        public static string GetFieldType(PropertyInfo property)
        {
            string detectedFieldTypename = GetFieldTypeNameFromAttribute(property);
            if (detectedFieldTypename != null) return detectedFieldTypename;

            if (PropertyIsLookup(property)) return nameof(FieldType.Lookup);
            Type propertyType = property.PropertyType;
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) propertyType = propertyType.GetGenericArguments()[0];

            if (propertyType.IsArray) propertyType = propertyType.GetElementType();


            if (propertyType.IsEnum)
                return nameof(FieldType.Choice);
            switch (Type.GetTypeCode(propertyType))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Double:
                case TypeCode.Single:
                    return nameof(FieldType.Number);
                case TypeCode.Decimal:
                    return nameof(FieldType.Currency);
                case TypeCode.Boolean:
                    return nameof(FieldType.Boolean);
                case TypeCode.String:
                case TypeCode.Char:
                    return nameof(FieldType.Text);
                case TypeCode.DateTime:
                    return nameof(FieldType.DateTime);

                default:
                    _log.Warn($"Cannot create fieldtype from {propertyType.Name}. Type is not supported.");
                    return nameof(FieldType.Invalid);
            }
        }

        private static string GetFieldTypeNameFromAttribute(PropertyInfo property)
        {
            string propertyName = property.Name;
            string detectedFieldType = null;
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<BooleanAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<ChoiceAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<CurrencyAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<DateTimeAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<LookupBaseAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<ManagedMetadataAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<NoteAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<NumberAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<TextAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<Attributes.FieldAttributes.UrlAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<UserAttribute>(property);
            return detectedFieldType;
        }

        private static string GetFieldTypeByAttribute<T>(PropertyInfo property) where T : Attribute
        {
            if (property.GetCustomAttribute(typeof(T), true) == null) return null;
            return (string)typeof(T).GetField(nameof(BooleanAttribute.AssociatedFieldType)).GetRawConstantValue();
        }

        public static object GetPropertyFromItemValue(PropertyInfo property, object itemValue)
        {
            Type propertyType = property.PropertyType;
            if (PropertyIsTaxonomy(property))
            {
                if (itemValue.GetType() == typeof(TaxonomyFieldValueCollection))
                {
                    var collection = (TaxonomyFieldValueCollection)itemValue;
                    return collection.ToDictionary(q => new Guid(q.TermGuid), q => q.Label);
                }
                else
                {
                    var item = (TaxonomyFieldValue)itemValue;
                    return new KeyValuePair<Guid, string>(new Guid(item.TermGuid), item.Label);
                }
            }
            else if (PropertyIsLookup(property))
            {
                if (itemValue.GetType().IsArray)
                {
                    var fieldLookupValues = (FieldLookupValue[])itemValue;
                    if (propertyType == typeof(Dictionary<int, string>)) return fieldLookupValues.ToDictionary(q => q.LookupId, q => q.LookupValue);
                    if (propertyType == typeof(int[])) return fieldLookupValues.Select(q => q.LookupId).ToArray();
                    Type elementType = propertyType.GetElementType();

                    if (elementType.GetProperty(AweCsomeField.SuffixId) != null)
                    {
                        var genericList = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType)) as IList;

                        var targetEntityObject = Activator.CreateInstance(elementType);
                        PropertyInfo idProperty = elementType.GetProperty(AweCsomeField.SuffixId);
                        PropertyInfo titleProperty = elementType.GetProperty(AweCsomeField.Title);

                        foreach (var fieldLookupValue in fieldLookupValues)
                        {
                            idProperty.SetValue(targetEntityObject, fieldLookupValue.LookupId);
                            if (titleProperty != null) titleProperty.SetValue(targetEntityObject, fieldLookupValue.LookupValue);
                            genericList.Add(targetEntityObject);
                        }

                        var array = Array.CreateInstance(elementType, genericList.Count);
                        genericList.CopyTo(array, 0);
                        return array;
                    }
                }
                else
                {
                    var fieldLookupValue = (FieldLookupValue)itemValue;
                    int lookupId = fieldLookupValue?.LookupId ?? 0;
                    string lookupValue = fieldLookupValue?.LookupValue;
                    if (propertyType == typeof(KeyValuePair<int, string>)) return new KeyValuePair<int, string>(lookupId, lookupValue);
                    if (propertyType == typeof(int)) return lookupId;
                    if (propertyType == typeof(string)) return lookupValue;

                    if (propertyType.GetProperty(AweCsomeField.SuffixId) != null)
                    {
                        var targetEntityObject = Activator.CreateInstance(propertyType);
                        PropertyInfo idProperty = propertyType.GetProperty(AweCsomeField.SuffixId);
                        PropertyInfo titleProperty = propertyType.GetProperty(AweCsomeField.Title);

                        idProperty.SetValue(targetEntityObject, fieldLookupValue.LookupId);
                        if (titleProperty != null) titleProperty.SetValue(targetEntityObject, fieldLookupValue.LookupValue);
                        return targetEntityObject;
                    }
                }
            } else if (PropertyIsUrl(property))
            {
                return ((FieldUrlValue)itemValue).Url;
            }
            if (propertyType.IsEnum)
            {
                return Enum.Parse(property.PropertyType, property.PropertyType.GetEnumInternalNameFromDisplayname(itemValue as string));
            }

            return itemValue;
        }

        public static Dictionary<string, string> GetEnumDisplaynames(this Type enumType)
        {
            var displayNames = new Dictionary<string, string>();

            foreach (var fieldname in Enum.GetNames(enumType))
            {
                var field = enumType.GetField(fieldname);
                var displayNameAttribute = field.GetCustomAttribute<DisplayNameAttribute>();

                displayNames.Add(field.Name, displayNameAttribute == null ? field.Name : displayNameAttribute.DisplayName);
            }
            return displayNames;
        }

        public static string GetEnumInternalNameFromDisplayname(this Type enumType, string displayname)
        {
            Dictionary<string, string> allDisplaynames = GetEnumDisplaynames(enumType);
            return allDisplaynames.First(q => q.Value == displayname).Key;
        }

        public static string GetEnumDisplayNameFromInternalname(this Type enumType, string internalName)
        {
            Dictionary<string, string> allDisplaynames = GetEnumDisplaynames(enumType);
            return allDisplaynames[internalName];
        }

        public static object ParseFromDisplayName(this Type enumType, string displayValue)
        {
            return Enum.Parse(enumType, GetEnumDisplaynames(enumType).First(q => q.Value == displayValue).Key);
        }

        public static PropertyInfo PropertyFromField(this Type entityType, string fieldName)
        {
            foreach (var property in entityType.GetProperties())            {
                if (GetInternalNameFromProperty(property) == fieldName) return property;
            }
            return null;
        }

   

        public static object GetItemValueFromProperty<T>(PropertyInfo property, T entity)
        {
            Type propertyType = property.PropertyType;
            if (PropertyIsLookup(property))
            {
                if (propertyType == typeof(KeyValuePair<int, string>)) return PropertyIsUser(property)
                        ? CreateUserFromId(((KeyValuePair<int, string>)property.GetValue(entity)).Key)
                        : CreateLookupFromId(((KeyValuePair<int, string>)property.GetValue(entity)).Key);
                if (propertyType == typeof(Dictionary<int, string>)) return PropertyIsUser(property)
                        ? CreateUsersFromIds(((Dictionary<int, string>)property.GetValue(entity))?.Select(q => q.Key).ToArray())
                        : CreateLookupsFromIds(((Dictionary<int, string>)property.GetValue(entity))?.Select(q => q.Key).ToArray());
                if (propertyType.IsArray && propertyType.GetElementType().GetProperty(AweCsomeField.SuffixId) != null)
                {
                    List<int> ids = new List<int>();
                    foreach (var item in (object[])property.GetValue(entity))
                    {
                        ids.Add((int)item.GetType().GetProperty(AweCsomeField.SuffixId).GetValue(item));
                    }
                    return PropertyIsUser(property) ? CreateUsersFromIds(ids.ToArray()) : CreateLookupsFromIds(ids.ToArray());
                }
                if (propertyType.GetProperty(AweCsomeField.SuffixId) != null)
                {
                    var item = property.GetValue(entity);
                    if (item == null) return null;
                    return CreateLookupFromId(((int)property.PropertyType.GetProperty(AweCsomeField.SuffixId).GetValue(item)));
                }
            }
    
            if (propertyType.IsEnum)
            {
                return property.PropertyType.GetEnumDisplayNameFromInternalname(property.GetValue(entity).ToString());
            }

            return property.GetValue(entity);
        }
    }
}
