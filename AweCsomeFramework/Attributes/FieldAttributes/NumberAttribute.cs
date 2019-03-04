using System;

namespace AweCsome.Attributes.FieldAttributes
{
    public class NumberAttribute : Attribute
    {
        private double? _min;
        private double? _max;
        private int? _numberOfDecimalPlaces;
        private double? _defaultValue;

        public bool MinHasValue { get { return _min.HasValue; } }
        public bool MaxHasValue { get { return _max.HasValue; } }
        public bool NumberOfDecimalPlacesHasValue { get { return _numberOfDecimalPlaces.HasValue; } }
        public bool DefaultValueHasValue { get { return _defaultValue.HasValue; } }

        public double Min { get { return _min ?? 0; } set { _min = value; } }
        public double Max { get { return _max ?? 0; } set { _max = value; } }
        public int NumberOfDecimalPlaces { get { return _numberOfDecimalPlaces ?? 0; } set { _numberOfDecimalPlaces = value; } }
        public double DefaultValue { get { return _min ?? 0; } set { _defaultValue = value; } }
        public bool ShowAsPercentage { get; set; }
        public const string AssociatedFieldType = "Number";
    }
}
