using validator.Models;

namespace validator.Services.Rules;

/// <summary>
/// Result of evaluating an individual field validation rule.
/// </summary>
/// <param name="RecordResult">The validation result record if one was produced (valid or invalid), or null if the rule passed cleanly.</param>
/// <param name="ShouldContinue">Whether subsequent rules for this field should be evaluated.</param>
public record FieldRuleResult(RecordResult? RecordResult, bool ShouldContinue);

/// <summary>
/// Defines a single field validation rule (null/required, length, allowed values, type).
/// </summary>
public interface IFieldRule
{
    FieldRuleResult Validate(int lineCount, int fieldIndex, string field, ValidationConfig config);
}
