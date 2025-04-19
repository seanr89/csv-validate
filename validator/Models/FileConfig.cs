
public record FileConfig(string fileType, string delimiter, bool headerLine, List<ValidationConfig> validationConfigs)
{
    public string FileType { get; } = fileType;
    public string Delimiter { get; } = delimiter;
    public bool HeaderLine { get; } = headerLine;

    public List<ValidationConfig> ValidationConfigs { get; } = validationConfigs;

    public override string ToString()
    {
        return $"FileType: {FileType}, ValidationConfigs: {string.Join(", ", ValidationConfigs)}";
    }
}