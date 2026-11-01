
namespace validator.Models.Settings;

public class RunSettings
{
    public string? InboxPrefix { get; set; } = "files";
    public string? OutboxPrefix { get; set; } = "files/outputs";
    public string? StorageType { get; set; } = "File";
}