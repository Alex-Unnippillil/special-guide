using System.Globalization;
using System.Windows.Controls;

namespace SpecialGuide.App.Validation;

public class NumericValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value == null)
        {
            return new ValidationResult(false, "Value is required");
        }

        return int.TryParse(value.ToString(), out _) 
            ? ValidationResult.ValidResult 
            : new ValidationResult(false, "Numeric value required");
    }
}
