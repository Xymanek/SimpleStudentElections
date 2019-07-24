using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SimpleStudentElections.Helpers
{
    /// <summary>
    /// Base class for validation rules that compare property value to something else. Requires the property to be
    /// <see cref="IComparable"/>
    /// </summary>
    public abstract class RelativeValidationBase : ValidationAttribute
    {
        public string MemberName { get; set; }

        private readonly Func<int, bool> _predicate;

        protected RelativeValidationBase(Func<int, bool> predicate)
        {
            _predicate = predicate;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (!(value is IComparable thisValue))
            {
                throw new Exception(
                    $"An error occurred during validation. Property {validationContext.MemberName} is not IComparable"
                );
            }

            object otherValue = GetOtherValue(validationContext);

            return _predicate(thisValue.CompareTo(otherValue))
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessageString, MemberName != null ? new[] {MemberName} : null);
        }

        protected abstract object GetOtherValue(ValidationContext validationContext);
    }

    /// <summary>
    /// Compares the property to another one on same model
    /// </summary>
    public class RelativeToOtherPropertyAttribute : RelativeValidationBase
    {
        private readonly string _otherPropertyName;

        public RelativeToOtherPropertyAttribute(string otherPropertyName, Func<int, bool> predicate) : base(predicate)
        {
            _otherPropertyName = otherPropertyName;
        }

        protected override object GetOtherValue(ValidationContext validationContext)
        {
            PropertyInfo otherProperty = validationContext.ObjectType.GetProperty(_otherPropertyName);
            if (otherProperty == null)
            {
                throw new Exception(
                    $"An error occurred during validation. Other property \"{_otherPropertyName}\" cannot be found"
                );
            }

            return otherProperty.GetValue(validationContext.ObjectInstance);
        }
    }

    public class GreaterThanProperty : RelativeToOtherPropertyAttribute
    {
        public GreaterThanProperty(string otherPropertyName)
            : base(otherPropertyName, ComparisonPredicates.GreaterThan)
        {
            ErrorMessage = "Must be greater than " + otherPropertyName;
        }
    }

    public class LessThanProperty : RelativeToOtherPropertyAttribute
    {
        public LessThanProperty(string otherPropertyName) : base(otherPropertyName, ComparisonPredicates.LessThan)
        {
            ErrorMessage = "Must be less than " + otherPropertyName;
        }
    }

    public class RelativeToValue : RelativeValidationBase
    {
        private object _referenceValue;
        private Func<object> _valueProvider;

        public RelativeToValue(object referenceValue, Func<int, bool> predicate) : base(predicate)
        {
            _referenceValue = referenceValue;
        }

        public RelativeToValue(Func<object> valueProvider, Func<int, bool> predicate) : base(predicate)
        {
            _valueProvider = valueProvider;
        }

        public object ReferenceValue
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_valueProvider != null)
                {
                    _referenceValue = _valueProvider();
                    _valueProvider = null;
                }

                return _referenceValue;
            }
        }

        protected override object GetOtherValue(ValidationContext validationContext) => ReferenceValue;
    }

    public class GreaterThan : RelativeToValue
    {
        public GreaterThan(object referenceValue) : base(referenceValue, ComparisonPredicates.GreaterThan)
        {
            SetDefaultErrorMessage();
        }

        public GreaterThan(Func<object> valueProvider) : base(valueProvider, ComparisonPredicates.GreaterThan)
        {
            SetDefaultErrorMessage();
        }

        private void SetDefaultErrorMessage()
        {
            ErrorMessage = "Must be greater than " + ReferenceValue;
        }
    }

    public class LessThan : RelativeToValue
    {
        public LessThan(object referenceValue) : base(referenceValue, ComparisonPredicates.LessThan)
        {
            SetDefaultErrorMessage();
        }

        public LessThan(Func<object> valueProvider) : base(valueProvider, ComparisonPredicates.LessThan)
        {
            SetDefaultErrorMessage();
        }

        private void SetDefaultErrorMessage()
        {
            ErrorMessage = "Must be less than " + ReferenceValue;
        }
    }

    internal static class ComparisonPredicates
    {
        public static readonly Func<int, bool> GreaterThan = i => i > 0;
        public static readonly Func<int, bool> LessThan = i => i < 0;
    }

    public class InFuture : GreaterThan
    {
        private static readonly Func<object> ValueProvider = () => DateTime.Now;

        public InFuture() : base(ValueProvider)
        {
            ErrorMessage = "Must be in future";
        }
    }
}