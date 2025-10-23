using System.Globalization;
using System.Windows.Controls;

namespace Agt.Desktop.Validation
{
    /// <summary>
    /// Jednoduchá validace čísla (parsovatelnost + volitelně min/max).
    /// </summary>
    public class NumberRangeRule : ValidationRule
    {
        public double? Min { get; set; }
        public double? Max { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var s = value as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(s)) return ValidationResult.ValidResult;

            if (!double.TryParse(s, NumberStyles.Float, cultureInfo, out var d))
                return new ValidationResult(false, "Není číslo.");

            if (Min.HasValue && d < Min.Value) return new ValidationResult(false, $"Min {Min.Value}");
            if (Max.HasValue && d > Max.Value) return new ValidationResult(false, $"Max {Max.Value}");

            return ValidationResult.ValidResult;
        }
    }
}
