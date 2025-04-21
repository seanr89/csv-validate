
public record ValidationConfig(string name, string type, int index, bool isNullable, int? minLength, int? maxLength, bool hasExpected, List<string> allowedValues, string errorMessage)
{
    public required string Name { get; set; } = name;
    public string? Type { get; set; } = type;
    public int Index { get; set; } = index;
    public bool IsNullable { get; set; } = isNullable;
    public int? minLength { get; set; } = minLength;
    public int? maxLength { get; set; } = maxLength;
    public bool HasExpected { get; set; } = hasExpected;
    public List<string> AllowedValues { get; set; } = allowedValues;
    public string ErrorMessage { get; set; } = errorMessage;
}