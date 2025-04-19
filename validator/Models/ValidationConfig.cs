
public class ValidationConfig{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public bool IsNullable { get; set; } = false;
    public int minLength { get; set; } = 0;
    public int maxLength { get; set; } = 255;
    public bool HasExpected { get; set; } = false;
    public List<string> AllowedValues { get; set; } = new List<string>();
}