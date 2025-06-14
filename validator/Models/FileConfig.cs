
/// <summary>
/// Base configuration for all file configuration/specifications
/// </summary>
/// <param name="fileType">The type of file</param>
/// <param name="delimiter">the csv type delimiter</param>
/// <param name="headerLine">if the csv file has a headerline</param>
/// <param name="validationConfigs">the array or file/column configurations</param>
/// <returns></returns>
public record FileConfig(string fileType, string delimiter, bool headerLine, List<ValidationConfig> validationConfigs)
{
    public string FileType { get; } = fileType;
    public string Delimiter { get; } = delimiter;
    public bool HeaderLine { get; } = headerLine;
    public bool HasFooter { get; } = false;

    public List<ValidationConfig> ValidationConfigs { get; } = validationConfigs;
}