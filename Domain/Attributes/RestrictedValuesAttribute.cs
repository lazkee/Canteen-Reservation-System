using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Attributes
{
    public class RestrictedValuesAttribute : ValidationAttribute
    {
        private readonly uint[] _allowedValues;

        public RestrictedValuesAttribute(params uint[] allowedValues)
        {
            _allowedValues = allowedValues;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is uint duration && Array.Exists(_allowedValues, v => v == duration))
            {
                return ValidationResult.Success!;
            }
            return new ValidationResult($"Duration must be one of: {string.Join(", ", _allowedValues)}");
        }
    }
}