using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SecureLink.Shared
{
    public class RequiredIfAttribute : ValidationAttribute
    {
        private readonly string _conditionProperty;
        private readonly object _expectedValue;

        public RequiredIfAttribute(string conditionProperty, object expectedValue)
        {
            _conditionProperty = conditionProperty;
            _expectedValue = expectedValue;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // Získání hodnoty podmíněné vlastnosti
            PropertyInfo conditionProperty = validationContext.ObjectType.GetProperty(_conditionProperty);
            if (conditionProperty == null)
            {
                return new ValidationResult($"Unknown property: {_conditionProperty}");
            }

            var conditionValue = conditionProperty.GetValue(validationContext.ObjectInstance);

            // Pokud podmínka platí (hodnota je očekávaná), pole nesmí být prázdné
            if (conditionValue?.Equals(_expectedValue) == true)
            {
                if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                {
                    return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} is required.");
                }
            }

            return ValidationResult.Success;
        }
    }
}
