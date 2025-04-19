
public record FileConfig(string fileType, List<ValidationConfig> validationConfigs)
{
    public string FileType { get; } = fileType;
    public List<ValidationConfig> ValidationConfigs { get; } = validationConfigs;

    public override string ToString()
    {
        return $"FileType: {FileType}, ValidationConfigs: {string.Join(", ", ValidationConfigs)}";
    }
}