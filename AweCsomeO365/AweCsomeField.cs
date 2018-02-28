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

        public void AddFieldToList(List sharePointList, PropertyInfo property)
        {
            Type propertyType = property.PropertyType;
            var ignoreOnCreationAttribute = propertyType.GetCustomAttribute<IgnoreOnCreationAttribute>();
            if (ignoreOnCreationAttribute != null && ignoreOnCreationAttribute.IgnoreOnCreation) return;
            var addToDefaultViewAttribute= propertyType.GetCustomAttribute<AddToDefaultViewAttribute>();
            string fieldXml = GetFieldCreationXml(property);
            Field field = sharePointList.Fields.AddFieldAsXml(fieldXml, addToDefaultViewAttribute != null, AddFieldOptions.AddFieldInternalNameHint);
        }

        private string GetFieldCreationXml(PropertyInfo property)
        {
            Type propertyType = property.PropertyType;

            string internalName = EntityHelper.GetInternalNameFromProperty(property);
            string displayName = EntityHelper.GetDisplayNameFromEntityType(property);
            string description = EntityHelper.GetDescriptionFromEntityType(propertyType);

            bool isRequired = PropertyIsRequired(property);
            bool isUnique = IsTrue(propertyType.GetCustomAttribute<UniqueAttribute>()?.IsUnique);
            FieldType fieldType = GetFieldType(propertyType);

            bool isMulti = IsMulti(propertyType);

            GetFieldCreationAdditionalXmlForFieldType(fieldType, propertyType, out string fieldAttributes, out string fieldAdditional);
            string fieldTypeString = fieldType.ToString();
            if (fieldAttributes == null) fieldAttributes = string.Empty;
            if (fieldAttributes == null) fieldAdditional = string.Empty;
            if (isMulti)
            {
                if (fieldType != FieldType.Choice) fieldAttributes += " Mult='TRUE'";
                fieldTypeString += "Multi";
            }

            string csomCreateCaml = $"<Field Type='{fieldTypeString}' Name='{internalName}' DisplayName='{displayName}' StaticName='{internalName}'";
            if (isRequired) csomCreateCaml += " Required='TRUE'";
            if (isUnique) csomCreateCaml += " EnforceUniqueValues='TRUE'";
            csomCreateCaml += $" {fieldAttributes}";
            if (string.IsNullOrWhiteSpace(fieldAdditional))
            {
                csomCreateCaml += "/>";
            }
            else
            {
                csomCreateCaml += ">";
                csomCreateCaml += fieldAdditional;
                csomCreateCaml += "</Field>";
            }
            return csomCreateCaml;
        }

        private void GetFieldCreationAdditionalXmlForFieldType(FieldType fieldType, Type propertyType, out string fieldAttributes, out string fieldAdditional)
        {
            fieldAttributes = string.Empty;
            fieldAdditional = string.Empty;
            switch (fieldType)
            {

                case FieldType.Boolean:
                    GetFieldCreationDetailsBoolean(propertyType, out fieldAdditional);
                    break;
                case FieldType.Choice:
                    GetFieldCreationDetailsChoice(propertyType, out fieldAttributes, out fieldAdditional);
                    break;
                case FieldType.Currency:
                    GetFieldCreationDetailsCurrency(propertyType, out fieldAttributes);
                    break;
                case FieldType.DateTime:
                    GetFieldCreationDetailsDateTime(propertyType, out fieldAttributes, out fieldAdditional);
                    break;
                case FieldType.Lookup:
                    GetFieldCreationDetailsLookup(propertyType, out fieldAttributes);
                    break;
                case FieldType.Note:
                    GetFieldCreationDetailsNote(propertyType, out fieldAttributes);
                    break;

                case FieldType.Number:
                    GetFieldCreationDetailsNumber(propertyType, out fieldAttributes);
                    break;
                case FieldType.Text:
                    GetFieldCreationDetailsText(propertyType, out fieldAttributes, out fieldAdditional);
                    break;
                case FieldType.URL:
                    GetFieldCreationDetailsUrl(propertyType, out fieldAttributes);
                    break;
                case FieldType.User:
                    GetFieldCreationDetailsUser(propertyType, out fieldAttributes);
                    break;
                default:
                    throw new NotImplementedException($"FieldType {fieldType} is unexpected and cannot be created");


            }
        }

        #region FieldCreationProperties


        private void GetFieldCreationDetailsBoolean(Type propertyType, out string fieldAdditional)
        {
            fieldAdditional = null;
            var booleanAttribute = propertyType.GetCustomAttribute<BooleanAttribute>();
            if (booleanAttribute != null) fieldAdditional = $"<Default>{(booleanAttribute.DefaultValue ? "1" : "0")}</Default>";
        }

        private void GetFieldCreationDetailsChoice(Type propertyType, out string fieldAttributes, out string fieldAdditional)
        {
            fieldAttributes = null;
            fieldAdditional = null;
            string[] choices = null;
            var choiceAttribute = propertyType.GetCustomAttribute<ChoiceAttribute>();
            if (choiceAttribute != null)
            {
                fieldAttributes = $"Format='{choiceAttribute.DisplayChoices}'";
                if (choiceAttribute.Choices != null) choices = choiceAttribute.Choices;
                if (choiceAttribute.DefaultValue != null) fieldAdditional = $"<Default>{choiceAttribute.DefaultValue}</Default>";
                if (choiceAttribute.AllowFillIn) fieldAttributes += " FillInChoice='TRUE'";
            }
            if (choices == null && propertyType.IsEnum) choices = Enum.GetNames(propertyType);
            string choiceXml = string.Empty;
            if (choices != null)
            {
                foreach (string choice in choices)
                {
                    choiceXml += $"<CHOICE>{choice}</CHOICE>";
                }
            }
            fieldAdditional += $"<CHOICES>{choiceXml}</CHOICES>";
        }

        private void GetFieldCreationDetailsCurrency(Type propertyType, out string fieldAttributes)
        {
            fieldAttributes = null;
            var currencyAttribute = propertyType.GetCustomAttribute<CurrencyAttribute>();
            if (currencyAttribute != null)
            {
                fieldAttributes = $"Commas='{(currencyAttribute.NumberOfDecimalPlaces == null || currencyAttribute.NumberOfDecimalPlaces == 0 ? "FALSE" : "TRUE")}'";
                if (currencyAttribute.Min.HasValue) fieldAttributes += $" Min={currencyAttribute.Min}";
                if (currencyAttribute.Max.HasValue) fieldAttributes += $" Max={currencyAttribute.Max}";
                if (currencyAttribute.CurrencyLocaleId != null) fieldAttributes += $" LCID='{currencyAttribute.CurrencyLocaleId}'";
            }
        }

        private void GetFieldCreationDetailsDateTime(Type propertyType, out string fieldAttributes, out string fieldAdditional)
        {
            fieldAttributes = null;
            fieldAdditional = null;

            var dateTimeAttribute = propertyType.GetCustomAttribute<DateTimeAttribute>();
            if (dateTimeAttribute != null)
            {
                fieldAttributes = $"Format='{dateTimeAttribute.DateTimeFormat}'";
                if (dateTimeAttribute.DefaultValue != null) fieldAdditional = $"<Default>{dateTimeAttribute.DefaultValue}</Default>";
                // TODO: FriendlyFormat
            }
        }

        private void GetFieldCreationDetailsLookup(Type propertyType, out string fieldAttributes)
        {
            var lookupAttribute = propertyType.GetCustomAttribute<LookupBaseAttribute>(true);
            // Can't be null (And if it is this SHOULD throw an error)
            fieldAttributes = $"List='{lookupAttribute.List}' ShowField='{lookupAttribute.Field}'";
        }

        private void GetFieldCreationDetailsNote(Type propertyType, out string fieldAttributes)
        {
            var noteAttribute = propertyType.GetCustomAttribute<NoteAttribute>();
            // Can't be null (And if it is this SHOULD throw an error)
            fieldAttributes = $"NumLines='{noteAttribute.NumberOfLinesForEditing}' RichText='{noteAttribute.AllowRichText}'";
            // TODO: AppendChangesToExistingText
        }

        private void GetFieldCreationDetailsNumber(Type propertyType, out string fieldAttributes)
        {
            fieldAttributes = null;
            var numberAttribute = propertyType.GetCustomAttribute<NumberAttribute>();
            if (numberAttribute != null)
            {
                fieldAttributes = $"Commas='{(numberAttribute.NumberOfDecimalPlaces == null || numberAttribute.NumberOfDecimalPlaces == 0 ? "FALSE" : "TRUE")}'";
                if (numberAttribute.Min.HasValue) fieldAttributes += $" Min={numberAttribute.Min}";
                if (numberAttribute.Max.HasValue) fieldAttributes += $" Max={numberAttribute.Max}";
                if (numberAttribute.ShowAsPercentage) fieldAttributes += $" Percentage='TRUE'";
            }
        }

        private void GetFieldCreationDetailsText(Type propertyType, out string fieldAttributes, out string fieldAdditional)
        {
            fieldAttributes = null;
            fieldAdditional = null;
            var textAttribute = propertyType.GetCustomAttribute<TextAttribute>();
            if (textAttribute != null)
            {
                fieldAttributes = $"MaxLength={textAttribute.MaxCharacters}";
                if (textAttribute.DefaultValue != null) fieldAdditional = $"<Default>{textAttribute.DefaultValue}</Default>";
            }
        }

        private void GetFieldCreationDetailsUrl(Type propertyType, out string fieldAttributes)
        {
            fieldAttributes = null;
            var urlAttribute = propertyType.GetCustomAttribute<UrlAttribute>();
            if (urlAttribute != null)
            {
                fieldAttributes = $"Format='{urlAttribute.UrlFieldFormatType}'";
            }
            else
            {
                fieldAttributes = $"Format='{nameof(UrlFieldFormatType.Hyperlink)}'";
            }
        }

        private void GetFieldCreationDetailsUser(Type propertyType, out string fieldAttributes)
        {
            fieldAttributes = null;
            var userAttribute = propertyType.GetCustomAttribute<UserAttribute>();
            if (userAttribute != null)
            {
                fieldAttributes = $"UserSelectionMode='{userAttribute.FieldUserSelectionMode}'";
                if (userAttribute.UserSelectionScope.HasValue) fieldAttributes += $" UserSelectionScope='{userAttribute.UserSelectionScope}'";
            }
        }

        #endregion FieldCreationProperties

        private bool IsMulti(Type propertyType)
        {
            return propertyType.IsArray || propertyType.IsGenericList() || propertyType.IsDictionary();
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
