using System.Diagnostics.CodeAnalysis;

namespace validator.Models;

/// <summary>
/// Represents the validation configuration for a single record field in a file.
/// Uses a required init model with a single set of PascalCase properties.
/// </summary>
public record ValidationConfig
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public int Index { get; init; }
    public bool IsNullable { get; init; }
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public List<string>? AllowedValues { get; init; }
    public string[]? Formats { get; init; }
    public string? ErrorMessage { get; init; }

    public ValidationConfig() { }

    [SetsRequiredMembers]
    public ValidationConfig(
        string name,
        string type,
        int index = 0,
        bool isNullable = false,
        int? minLength = null,
        int? maxLength = null,
        List<string>? allowedValues = null,
        string[]? formats = null,
        string? errorMessage = null)
    {
        Name = name;
        Type = type;
        Index = index;
        IsNullable = isNullable;
        MinLength = minLength;
        MaxLength = maxLength;
        AllowedValues = allowedValues ?? [];
        Formats = formats ?? [];
        ErrorMessage = errorMessage;
    }
}
