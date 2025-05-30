
public record FileConfig(string fileType, string delimiter, bool headerLine, List<ValidationConfig> validationConfigs)
{
    public string FileType { get; } = fileType;
    public string Delimiter { get; } = delimiter;
    public bool HeaderLine { get; } = headerLine;
    public bool HasFooter { get; } = false;

    public List<ValidationConfig> ValidationConfigs { get; } = validationConfigs;
}