using validator.Models;
using validator.Services.Interfaces;

namespace validator.Services.Rules;

/// <summary>
/// Helper for formatting descriptive validation error messages.
/// </summary>
public static class RuleMessageFormatter
{
    public static string FormatErrorMessage(int fieldIndex, string fieldName, string value, string specificDetail, string? configuredMessage)
    {
        var prefix = $"Field '{fieldName}' at index {fieldIndex}";
        if (!string.IsNullOrWhiteSpace(configuredMessage))
        {
            return $"{prefix}: {specificDetail}. {configuredMessage}";
        }
        return $"{prefix}: {specificDetail}.";
    }
}

/// <summary>
/// Validates null and empty constraints for a field.
/// </summary>
public class RequiredFieldRule : IFieldRule
{
    public FieldRuleResult Validate(int lineCount, int fieldIndex, string field, ValidationConfig config)
    {
        if (string.IsNullOrEmpty(field))
        {
            if (!config.IsNullable)
            {
                var message = RuleMessageFormatter.FormatErrorMessage(
                    fieldIndex, config.Name, field, "Field is required and cannot be empty", config.ErrorMessage);
                return new FieldRuleResult(new RecordResult(lineCount, fieldIndex, config.Name, field, false, message), false);
            }

            // Valid: nullable and empty - records a passing result and stops subsequent checks
            return new FieldRuleResult(new RecordResult(lineCount, fieldIndex, config.Name, field, true, string.Empty), false);
        }

        return new FieldRuleResult(null, true);
    }
}

/// <summary>
/// Validates minimum and maximum string length constraints.
/// </summary>
public class LengthFieldRule : IFieldRule
{
    public FieldRuleResult Validate(int lineCount, int fieldIndex, string field, ValidationConfig config)
    {
        var minLength = config.MinLength;
        var maxLength = config.MaxLength;

        if (minLength != null && field.Length < minLength)
        {
            var message = RuleMessageFormatter.FormatErrorMessage(
                fieldIndex, config.Name, field,
                $"Value '{field}' length ({field.Length}) is less than minimum length ({minLength})",
                config.ErrorMessage);
            return new FieldRuleResult(new RecordResult(lineCount, fieldIndex, config.Name, field, false, message), false);
        }

        if (maxLength != null && field.Length > maxLength)
        {
            var message = RuleMessageFormatter.FormatErrorMessage(
                fieldIndex, config.Name, field,
                $"Value '{field}' length ({field.Length}) exceeds maximum length ({maxLength})",
                config.ErrorMessage);
            return new FieldRuleResult(new RecordResult(lineCount, fieldIndex, config.Name, field, false, message), false);
        }

        return new FieldRuleResult(null, true);
    }
}

/// <summary>
/// Validates whether the field value belongs to the allowed values set.
/// </summary>
public class AllowedValuesFieldRule : IFieldRule
{
    public FieldRuleResult Validate(int lineCount, int fieldIndex, string field, ValidationConfig config)
    {
        var allowedValues = config.AllowedValues;
        if (allowedValues != null && allowedValues.Count > 0 && !allowedValues.Contains(field))
        {
            var message = RuleMessageFormatter.FormatErrorMessage(
                fieldIndex, config.Name, field,
                $"Value '{field}' is not in allowed values: [{string.Join(", ", allowedValues)}]",
                config.ErrorMessage);
            return new FieldRuleResult(new RecordResult(lineCount, fieldIndex, config.Name, field, false, message), false);
        }

        return new FieldRuleResult(null, true);
    }
}

/// <summary>
/// Validates field data type using the injected type validator.
/// </summary>
public class TypeFieldRule : IFieldRule
{
    private readonly ITypeValidator _typeValidator;

    public TypeFieldRule(ITypeValidator typeValidator)
    {
        _typeValidator = typeValidator;
    }

    public FieldRuleResult Validate(int lineCount, int fieldIndex, string field, ValidationConfig config)
    {
        var expectedType = config.Type ?? string.Empty;
        var formats = config.Formats;

        bool typeResult = _typeValidator.ValidateType(field, expectedType, formats);
        if (!typeResult)
        {
            var detail = $"Value '{field}' is not a valid {expectedType}";
            var message = RuleMessageFormatter.FormatErrorMessage(
                fieldIndex, config.Name, field, detail, config.ErrorMessage);
            return new FieldRuleResult(new RecordResult(lineCount, fieldIndex, config.Name, field, false, message), false);
        }

        return new FieldRuleResult(null, true);
    }
}
