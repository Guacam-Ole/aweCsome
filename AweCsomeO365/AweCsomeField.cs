using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using AweCsomeO365.Attributes.FieldAttributes;
using AweCsomeO365.Attributes.IgnoreAttributes;

namespace AweCsomeO365
{
    public class AweCsomeField : IAweCsomeField
    {

        public void AddFieldToList(ClientContext clientContext, List sharePointList, PropertyInfo property)
        {
            Type propertyType = property.PropertyType;
            var ignoreOnCreationAttribute = propertyType.GetCustomAttribute<IgnoreOnCreationAttribute>();
            if (ignoreOnCreationAttribute != null && ignoreOnCreationAttribute.IgnoreOnCreation) return;

            string fieldXml = GetFieldCreationXml(property);
        }

        private string GetFieldCreationXml(PropertyInfo property)
        {
            Type propertyType = property.PropertyType;

            string internalName = EntityHelper.GetInternalNameFromEntityType(propertyType);
            string displayName = EntityHelper.GetDisplayNameFromEntityType(propertyType);
            string description = EntityHelper.GetDescriptionFromEntityType(propertyType);

            bool isRequired = PropertyIsRequired(property);
            bool isUnique = IsTrue(propertyType.GetCustomAttribute<UniqueAttribute>()?.IsUnique);
            FieldType fieldType = GetFieldType(propertyType);

            return null;
            // TODO: Continue here
        }

        private FieldType GetFieldType(Type propertyType)
        {
            if (PropertyTypeIsLookup(propertyType)) return FieldType.Lookup;

            if (propertyType.IsArray) propertyType = propertyType.GetElementType();
            FieldType? detectedFieldType = GetFieldTypeFromAttribute(propertyType);
            if (detectedFieldType != null) return detectedFieldType.Value;

            if (propertyType.IsEnum) return FieldType.Choice;
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
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return FieldType.Number;
                case TypeCode.Boolean:
                    return FieldType.Boolean;
                case TypeCode.String:
                    return FieldType.Text;
                case TypeCode.DateTime:
                    return FieldType.DateTime;
                default:
                    return FieldType.Invalid;
            }
        }

        private FieldType? GetFieldTypeFromAttribute(Type propertyType)
        {
            FieldType? detectedFieldType = null;
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<BooleanAttribute>(propertyType);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<ChoiceAttribute>(propertyType);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<CurrencyAttribute>(propertyType);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<DateTimeAttribute>(propertyType);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<LookupBaseAttribute>(propertyType);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<ManagedMetadataAttribute>(propertyType);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<NoteAttribute>(propertyType);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<NumberAttribute>(propertyType);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<TextAttribute>(propertyType);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<UrlAttribute>(propertyType);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<UserAttribute>(propertyType);
            return detectedFieldType;
        }

        private FieldType? GetFieldTypeByAttribute<T>(Type propertyType) where T : Attribute
        {
            if (propertyType.GetCustomAttribute(typeof(T), true) == null) return null;
            return (FieldType)typeof(T).GetField(nameof(BooleanAttribute.AssociatedFieldType)).GetRawConstantValue();
        }

        private bool PropertyTypeIsLookup(Type propertyType)
        {
            if (propertyType.GetCustomAttribute<LookupBaseAttribute>(true) != null) return true;
            if (propertyType == typeof(KeyValuePair<int, string>)) return true; // Single-Lookup
            if (propertyType == typeof(Dictionary<int, string>)) return true; // Multi-Lookup
            if (propertyType.GetProperty("Id") != null) return true; // Single Lookup with complex type
            if (propertyType.IsArray && propertyType.GetElementType().GetProperty("Id") != null) return true; // Multi Lookup with complex type
            return false;
        }

        private bool IsTrue(bool? value)
        {
            return value.HasValue && value.Value;
        }

        private bool PropertyIsRequired(PropertyInfo property)
        {
            var isRequiredAttribute = property.GetCustomAttribute<RequiredAttribute>();
            if (isRequiredAttribute != null) return isRequiredAttribute.IsRequired;

            return (Nullable.GetUnderlyingType(property.PropertyType) == null); // property isn't nullable
        }

        private string GetEnumCaml(Type enumType)
        {
            string[] enumValues = Enum.GetNames(enumType);
            string enumCaml = "<CHOICES>";
            foreach (var enumValue in enumValues)
            {
                enumCaml += $"<CHOICE>{enumValue}</CHOICE>";
            }
            enumCaml += "</CHOICES>\n";
            return enumCaml;
        }

    }
}
