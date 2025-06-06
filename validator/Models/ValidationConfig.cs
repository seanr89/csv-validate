
public record ValidationConfig(string name, string type, int index, bool isNullable, int? minLength, int? maxLength, bool hasExpected, List<string> allowedValues, string[] formats, string errorMessage)
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