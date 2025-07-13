

namespace validator.Models;

/// <summary>
/// Represents the validation configuration for a single record type in a file.
/// </summary>
/// <param name="name">The name of the field to validate.</param>
/// <param name="type">The data type of the field.</param>
/// <param name="index">The zero-based index of the field in the record.</param>
/// <param name="isNullable">Indicates whether the field is nullable.</param>
/// <param name="minLength">The minimum allowed length for the field value, if applicable.</param>
/// <param name="maxLength">The maximum allowed length for the field value, if applicable.</param>
/// <param name="hasExpected">Indicates whether the field has an expected value to validate against.</param>
/// <param name="allowedValues">A list of allowed values for the field.</param>
/// <param name="formats">An array of valid formats for the field value.</param>
/// <param name="errorMessage">The error message to display if validation fails.</param>
public record ValidationConfig(string name, string type, int index, bool isNullable,
    int? minLength, int? maxLength, bool hasExpected, List<string> allowedValues, string[] formats, string errorMessage)
{
    public required string Name { get; set; } = name;
    public string? Type { get; set; } = type;
    public int Index { get; set; } = index;
    public bool IsNullable { get; set; } = isNullable;
    public int? MinLength { get; set; } = minLength;
    public int? MaxLength { get; set; } = maxLength;
    public bool HasExpected { get; set; } = hasExpected;
    public string[] Formats { get; set; } = formats;
    public List<string> AllowedValues { get; set; } = allowedValues;
    public string ErrorMessage { get; set; } = errorMessage;
}