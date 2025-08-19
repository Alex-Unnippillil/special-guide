using System.Globalization;
using System.Windows.Controls;

namespace SpecialGuide.App.Validation;

public class NotEmptyValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            return ValidationResult.ValidResult;
        }

        return new ValidationResult(false, "Value cannot be empty");
    }
}
