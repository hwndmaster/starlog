using System.Globalization;
using System.Windows.Controls;

namespace Genius.Starlog.UI.Validation;

public sealed class ValueRangeValidationRule<TValue> : ValidationRule
    where TValue : notnull, IComparable
{
    private readonly Func<TValue> _valueFrom;
    private readonly Func<TValue> _valueTo;

    public ValueRangeValidationRule(Func<TValue> valueFrom, Func<TValue> valueTo)
    {
        _valueFrom = valueFrom;
        _valueTo = valueTo;
    }

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        var valueFrom = _valueFrom();
        var valueTo = _valueTo();

        if (valueFrom.CompareTo(valueTo) > 0)
        {
            return new ValidationResult(false, "The value from must be lower than the value to.");
        }

        return ValidationResult.ValidResult;
    }
}
