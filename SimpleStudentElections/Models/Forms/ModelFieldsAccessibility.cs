using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace SimpleStudentElections.Models.Forms
{
    public class ModelFieldsAccessibility
    {
        public enum Kind
        {
            /// <summary>
            /// Fully interactable by user
            /// </summary>
            Editable,

            /// <summary>
            /// Read-only field
            /// </summary>
            NotEditable,

            /// <summary>
            /// Specifies that field is a complex type and has sub-fields whose accessibility must be checked
            /// </summary>
            ComplexType,

            /// <summary>
            /// This field should not exist in user's current view
            /// </summary>
            NotShown
        }

        private class FieldInfo
        {
            public Kind Kind;
            public ModelFieldsAccessibility SubFieldInfo;
        }

        private Kind? _defaultKind;
        private readonly IDictionary<string, FieldInfo> _dictionary = new Dictionary<string, FieldInfo>();

        public Kind this[string field] => GetFieldInfo(field).Kind;

        public Kind? DefaultKind
        {
            get => _defaultKind;
            set
            {
                if (value == Kind.ComplexType)
                {
                    throw new Exception(nameof(DefaultKind) + " cannot be " + nameof(Kind.ComplexType));
                }

                _defaultKind = value;
            }
        }

        public void MarkNotShown(string fieldName)
        {
            EnsureValidFieldName(fieldName);
            _dictionary[fieldName] = new FieldInfo() {Kind = Kind.NotShown};
        }

        public void MarkComplex(string fieldName, ModelFieldsAccessibility subFieldsInfo)
        {
            EnsureValidFieldName(fieldName);
            _dictionary[fieldName] = new FieldInfo()
            {
                Kind = Kind.ComplexType,
                SubFieldInfo = subFieldsInfo
            };
        }

        public void MarkComplexBatch(
            Func<ModelFieldsAccessibility> subFieldsInfoGenerator,
            IEnumerable<string> fieldNames
        )
        {
            foreach (string fieldName in fieldNames)
            {
                MarkComplex(fieldName, subFieldsInfoGenerator());
            }
        }

        public void MarkNotEditable(string fieldName)
        {
            EnsureValidFieldName(fieldName);
            _dictionary[fieldName] = new FieldInfo() {Kind = Kind.NotEditable};
        }

        public void MarkEditable(string fieldName)
        {
            EnsureValidFieldName(fieldName);
            _dictionary[fieldName] = new FieldInfo() {Kind = Kind.Editable};
        }

        private static void EnsureValidFieldName(string fieldName, string argumentName = "fieldName")
        {
            if (!fieldName.All(char.IsLetterOrDigit))
            {
                throw new ArgumentOutOfRangeException(
                    argumentName, fieldName, "Field name cannot contain special characters"
                );
            }
        }

        private static readonly Kind[] VisibleKinds = {Kind.Editable, Kind.NotEditable, Kind.ComplexType};

        public bool IsVisible(string fieldName)
        {
            return VisibleKinds.Contains(this[fieldName]);
        }

        public bool IsComplex(string fieldName)
        {
            return this[fieldName] == Kind.ComplexType;
        }

        private static readonly Kind[] ChangeableByUser = {Kind.Editable, Kind.ComplexType};

        public bool CanBeChangedByUser(string fieldName)
        {
            return ChangeableByUser.Contains(this[fieldName]);
        }

        public ModelFieldsAccessibility GetSubFieldInfo(string fieldName)
        {
            if (!IsComplex(fieldName))
            {
                throw new Exception(nameof(GetSubFieldInfo) + " can be called only when the field is complex");
            }

            return GetFieldInfo(fieldName).SubFieldInfo;
        }

        public IEnumerable<string> GetAllDefinedFields()
        {
            return _dictionary.Keys;
        }

        private FieldInfo GetFieldInfo(string fieldName)
        {
            EnsureValidFieldName(fieldName);

            if (_dictionary.TryGetValue(fieldName, out FieldInfo fieldInfo))
            {
                return fieldInfo;
            }

            if (DefaultKind.HasValue)
            {
                return new FieldInfo()
                {
                    Kind = DefaultKind.Value
                };
            }
            
            throw new ArgumentOutOfRangeException(nameof(fieldInfo));
        }

        public void ReplaceUneditableWithOldValues(object current, object original)
        {
            EnsureAllowedDefaultKind(Kind.Editable, nameof(ReplaceUneditableWithOldValues));
            Type type = current.GetType();

            if (original.GetType() != type)
            {
                throw new ArgumentException($"{nameof(current)} and {nameof(original)} must be of same class");
            }

            foreach (string fieldName in GetAllDefinedFields())
            {
                PropertyInfo propertyInfo = type.GetProperty(fieldName);
                Debug.Assert(propertyInfo != null, $"Field {fieldName} does not exist");

                if (!CanBeChangedByUser(fieldName))
                {
                    // Not editable at all, just replace with old value
                    object originalValue = propertyInfo.GetValue(original);
                    propertyInfo.SetValue(current, originalValue);
                }
                else if (IsComplex(fieldName))
                {
                    // Sub-fields can be resticted
                    object currentValue = propertyInfo.GetValue(current);
                    object originalValue = propertyInfo.GetValue(original);

                    GetSubFieldInfo(fieldName).ReplaceUneditableWithOldValues(currentValue, originalValue);
                }

                // Else we keep the current value as it is, since the user can edit it
            }
        }

        public IEnumerable<string> IgnoredValidationPaths
        {
            get
            {
                EnsureAllowedDefaultKind(Kind.Editable, nameof(ReplaceUneditableWithOldValues));
                List<string> ignoredPaths = new List<string>();

                foreach (string fieldName in GetAllDefinedFields())
                {
                    if (!CanBeChangedByUser(fieldName))
                    {
                        ignoredPaths.Add(fieldName);
                    }
                    else if (IsComplex(fieldName))
                    {
                        IEnumerable<string> ignoredFromSubField = GetSubFieldInfo(fieldName)
                            .IgnoredValidationPaths
                            .Select(subFieldName => fieldName + "." + subFieldName);

                        ignoredPaths.AddRange(ignoredFromSubField);
                    }
                }

                return ignoredPaths;
            }
        }

        public void EnsureAllowedDefaultKind(Kind allowed, string where)
        {
            if (DefaultKind != null && DefaultKind != allowed)
            {
                throw new Exception(
                    $"Only {allowed} is supported as default kind in {where}"
                );
            }
        }
    }
}