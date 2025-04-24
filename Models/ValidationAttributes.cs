using System.ComponentModel.DataAnnotations;

namespace HomeownersSubdivision.Models
{
    // Custom validation attribute for boolean fields that must be true
    public class MustBeTrueAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            return value is bool boolValue && boolValue;
        }
    }

    // Custom validation attribute for conditional required fields
    public class RequiredIfAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;
        private readonly object _comparisonValue;

        public RequiredIfAttribute(string comparisonProperty, object comparisonValue)
        {
            _comparisonProperty = comparisonProperty;
            _comparisonValue = comparisonValue;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var propertyInfo = validationContext.ObjectType.GetProperty(_comparisonProperty);
            if (propertyInfo == null)
                return new ValidationResult($"Unknown property: {_comparisonProperty}");

            var propertyValue = propertyInfo.GetValue(validationContext.ObjectInstance, null);
            
            if (propertyValue?.ToString() == _comparisonValue.ToString())
            {
                if (value == null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue)))
                {
                    return new ValidationResult(ErrorMessage);
                }
            }

            return ValidationResult.Success;
        }
    }
} 