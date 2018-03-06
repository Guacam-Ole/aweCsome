using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using AweCsomeO365.Attributes.FieldAttributes;
using AweCsomeO365.Attributes.IgnoreAttributes;
using log4net;

namespace AweCsomeO365
{
    public class AweCsomeField : IAweCsomeField
    {
        private ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SuffixId = "Id";
        private const string SuffixIds = "Ids";


        public void AddFieldToList(List sharePointList, PropertyInfo property, Dictionary<string, Guid> lookupTableIds)
        {
            var ignoreOnCreationAttribute = property.GetCustomAttribute<IgnoreOnCreationAttribute>();
            if (ignoreOnCreationAttribute != null && ignoreOnCreationAttribute.IgnoreOnCreation) return;
            var addToDefaultViewAttribute = property.GetCustomAttribute<AddToDefaultViewAttribute>();
            string fieldXml = GetFieldCreationXml(property, lookupTableIds);
            Field field = sharePointList.Fields.AddFieldAsXml(fieldXml, addToDefaultViewAttribute != null, AddFieldOptions.AddFieldInternalNameHint);
        }

        private void RemoveSuffixFromName(ref string name, string suffix)
        {
            if (name == null) return;
            if (name.EndsWith(suffix)) name = name.Substring(0, name.Length - suffix.Length);
        }

        private void RemoveLookupIdFromFieldName(ref string internalName, ref string displayName)
        {
            RemoveSuffixFromName(ref internalName, SuffixIds);
            RemoveSuffixFromName(ref internalName, SuffixId);

            RemoveSuffixFromName(ref displayName, SuffixIds);
            RemoveSuffixFromName(ref displayName, SuffixId);
        }

        private string GetFieldCreationXml(PropertyInfo property, Dictionary<string, Guid> lookupTableIds)
        {
            Type propertyType = property.PropertyType;

            string internalName = EntityHelper.GetInternalNameFromProperty(property);
            string displayName = EntityHelper.GetDisplayNameFromEntityType(property);
            string description = EntityHelper.GetDescriptionFromEntityType(propertyType);

            bool isRequired = PropertyIsRequired(property);
            bool isUnique = IsTrue(propertyType.GetCustomAttribute<UniqueAttribute>()?.IsUnique);
            FieldType fieldType = GetFieldType(property);
            if (fieldType == FieldType.Lookup) RemoveLookupIdFromFieldName(ref internalName, ref displayName);

            bool isMulti = IsMulti(propertyType);

            GetFieldCreationAdditionalXmlForFieldType(fieldType, property, lookupTableIds, out string fieldAttributes, out string fieldAdditional);
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

        private void GetFieldCreationAdditionalXmlForFieldType(FieldType fieldType, PropertyInfo property, Dictionary<string, Guid> lookupTableIds, out string fieldAttributes, out string fieldAdditional)
        {
            fieldAttributes = string.Empty;
            fieldAdditional = string.Empty;
            switch (fieldType)
            {

                case FieldType.Boolean:
                    GetFieldCreationDetailsBoolean(property, out fieldAdditional);
                    break;
                case FieldType.Choice:
                    GetFieldCreationDetailsChoice(property, out fieldAttributes, out fieldAdditional);
                    break;
                case FieldType.Currency:
                    GetFieldCreationDetailsCurrency(property, out fieldAttributes);
                    break;
                case FieldType.DateTime:
                    GetFieldCreationDetailsDateTime(property, out fieldAttributes, out fieldAdditional);
                    break;
                case FieldType.Lookup:
                    GetFieldCreationDetailsLookup(property, lookupTableIds, out fieldAttributes);
                    break;
                case FieldType.Note:
                    GetFieldCreationDetailsNote(property, out fieldAttributes);
                    break;

                case FieldType.Number:
                    GetFieldCreationDetailsNumber(property, out fieldAttributes);
                    break;
                case FieldType.Text:
                    GetFieldCreationDetailsText(property, out fieldAttributes, out fieldAdditional);
                    break;
                case FieldType.URL:
                    GetFieldCreationDetailsUrl(property, out fieldAttributes);
                    break;
                case FieldType.User:
                    GetFieldCreationDetailsUser(property, out fieldAttributes);
                    break;
                default:
                    throw new NotImplementedException($"FieldType {fieldType} is unexpected and cannot be created");


            }
        }

        #region FieldCreationProperties


        private void GetFieldCreationDetailsBoolean(PropertyInfo property, out string fieldAdditional)
        {
            fieldAdditional = null;
            var booleanAttribute = property.GetCustomAttribute<BooleanAttribute>();
            if (booleanAttribute != null) fieldAdditional = $"<Default>{(booleanAttribute.DefaultValue ? "1" : "0")}</Default>";
        }

        private void GetFieldCreationDetailsChoice(PropertyInfo property, out string fieldAttributes, out string fieldAdditional)
        {
            fieldAttributes = null;
            fieldAdditional = null;
            string[] choices = null;
            var choiceAttribute = property.GetCustomAttribute<ChoiceAttribute>();
            if (choiceAttribute != null)
            {
                fieldAttributes = $"Format='{choiceAttribute.DisplayChoices}'";
                if (choiceAttribute.Choices != null) choices = choiceAttribute.Choices;
                if (choiceAttribute.DefaultValue != null) fieldAdditional = $"<Default>{choiceAttribute.DefaultValue}</Default>";
                if (choiceAttribute.AllowFillIn) fieldAttributes += " FillInChoice='TRUE'";
            }
            Type propertyType = property.PropertyType;
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

        private void GetFieldCreationDetailsCurrency(PropertyInfo property, out string fieldAttributes)
        {
            fieldAttributes = null;
            var currencyAttribute = property.GetCustomAttribute<CurrencyAttribute>();
            if (currencyAttribute != null)
            {
                fieldAttributes = $"Commas='{(currencyAttribute.NumberOfDecimalPlaces == null || currencyAttribute.NumberOfDecimalPlaces == 0 ? "FALSE" : "TRUE")}'";
                if (currencyAttribute.Min.HasValue) fieldAttributes += $" Min={currencyAttribute.Min}";
                if (currencyAttribute.Max.HasValue) fieldAttributes += $" Max={currencyAttribute.Max}";
                if (currencyAttribute.CurrencyLocaleId != null) fieldAttributes += $" LCID='{currencyAttribute.CurrencyLocaleId}'";
            }
        }

        private void GetFieldCreationDetailsDateTime(PropertyInfo property, out string fieldAttributes, out string fieldAdditional)
        {
            fieldAttributes = null;
            fieldAdditional = null;

            var dateTimeAttribute = property.GetCustomAttribute<DateTimeAttribute>();
            if (dateTimeAttribute != null)
            {
                fieldAttributes = $"Format='{dateTimeAttribute.DateTimeFormat}'";
                if (dateTimeAttribute.DefaultValue != null) fieldAdditional = $"<Default>{dateTimeAttribute.DefaultValue}</Default>";
                // TODO: FriendlyFormat
            }
        }

        public static string GetLookupListName(PropertyInfo property, out string fieldname)
        {
            fieldname = "Title";
            var lookupAttribute = property.GetCustomAttribute<LookupBaseAttribute>(true);
            if (lookupAttribute == null)
            {
                Type propertyType = property.PropertyType;

                if (propertyType.IsArray)
                {
                    propertyType = propertyType.GetElementType();
                }
                if (propertyType.GetProperty(SuffixId) != null)
                {
                    return propertyType.Name;
                }
            }
            else
            {
                fieldname = lookupAttribute.Field;
                return lookupAttribute.List;
            }
            return null;
        }

        private void GetFieldCreationDetailsLookup(PropertyInfo property, Dictionary<string, Guid> lookupTableIds, out string fieldAttributes)
        {
            string list = GetLookupListName(property, out string field);
            if (list == null) throw new Exception("Missing list-information for Lookup-Field");
            fieldAttributes = $"List='{lookupTableIds[list]}' ShowField='{field}'";
        }

        private void GetFieldCreationDetailsNote(PropertyInfo property, out string fieldAttributes)
        {
            var noteAttribute = property.GetCustomAttribute<NoteAttribute>();
            // Can't be null (And if it is this SHOULD throw an error)
            fieldAttributes = $"NumLines='{noteAttribute.NumberOfLinesForEditing}' RichText='{noteAttribute.AllowRichText}'";
            // TODO: AppendChangesToExistingText
        }

        private void GetFieldCreationDetailsNumber(PropertyInfo property, out string fieldAttributes)
        {
            fieldAttributes = null;
            var numberAttribute = property.GetCustomAttribute<NumberAttribute>();
            if (numberAttribute != null)
            {
                fieldAttributes = $"Commas='{(numberAttribute.NumberOfDecimalPlaces == null || numberAttribute.NumberOfDecimalPlaces == 0 ? "FALSE" : "TRUE")}'";
                if (numberAttribute.Min.HasValue) fieldAttributes += $" Min={numberAttribute.Min}";
                if (numberAttribute.Max.HasValue) fieldAttributes += $" Max={numberAttribute.Max}";
                if (numberAttribute.ShowAsPercentage) fieldAttributes += $" Percentage='TRUE'";
            }
        }

        private void GetFieldCreationDetailsText(PropertyInfo property, out string fieldAttributes, out string fieldAdditional)
        {
            fieldAttributes = null;
            fieldAdditional = null;
            var textAttribute = property.GetCustomAttribute<TextAttribute>();
            if (textAttribute != null)
            {
                fieldAttributes = $"MaxLength={textAttribute.MaxCharacters}";
                if (textAttribute.DefaultValue != null) fieldAdditional = $"<Default>{textAttribute.DefaultValue}</Default>";
            }
        }

        private void GetFieldCreationDetailsUrl(PropertyInfo property, out string fieldAttributes)
        {
            fieldAttributes = null;
            var urlAttribute = property.GetCustomAttribute<UrlAttribute>();
            if (urlAttribute != null)
            {
                fieldAttributes = $"Format='{urlAttribute.UrlFieldFormatType}'";
            }
            else
            {
                fieldAttributes = $"Format='{nameof(UrlFieldFormatType.Hyperlink)}'";
            }
        }

        private void GetFieldCreationDetailsUser(PropertyInfo property, out string fieldAttributes)
        {
            fieldAttributes = null;
            var userAttribute = property.GetCustomAttribute<UserAttribute>();
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

        private FieldType GetFieldType(PropertyInfo property)
        {
            if (PropertyIsLookup(property)) return FieldType.Lookup;
            Type propertyType = property.PropertyType;

            if (propertyType.IsArray) propertyType = propertyType.GetElementType();
            FieldType? detectedFieldType = GetFieldTypeFromAttribute(property);
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

        private FieldType? GetFieldTypeFromAttribute(PropertyInfo property)
        {
            FieldType? detectedFieldType = null;
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<BooleanAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<ChoiceAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<CurrencyAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<DateTimeAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<LookupBaseAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<ManagedMetadataAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<NoteAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<NumberAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<TextAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<UrlAttribute>(property);
            detectedFieldType = detectedFieldType ?? GetFieldTypeByAttribute<UserAttribute>(property);
            return detectedFieldType;
        }

        private FieldType? GetFieldTypeByAttribute<T>(PropertyInfo property) where T : Attribute
        {
            if (property.GetCustomAttribute(typeof(T), true) == null) return null;
            return (FieldType)typeof(T).GetField(nameof(BooleanAttribute.AssociatedFieldType)).GetRawConstantValue();
        }

        public static bool PropertyIsLookup(PropertyInfo property)
        {
            if (property.GetCustomAttribute<LookupBaseAttribute>(true) != null) return true;
            Type propertyType = property.PropertyType;
            if (propertyType == typeof(KeyValuePair<int, string>)) return true; // Single-Lookup
            if (propertyType == typeof(Dictionary<int, string>)) return true; // Multi-Lookup
            if (propertyType.GetProperty(SuffixId) != null) return true; // Single Lookup with complex type
            if (propertyType.IsArray && propertyType.GetElementType().GetProperty(SuffixId) != null) return true; // Multi Lookup with complex type
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

            Type propertyType = property.PropertyType;
            if (propertyType.IsGenericType)
            {
                return propertyType.GetGenericTypeDefinition() != typeof(Nullable<>);
            }
            return false;
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
