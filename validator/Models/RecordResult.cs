
namespace validator.Models;

public class RecordResult(
    int recordCount,
    string key,
    bool valid,
    string? errorMessage
)
{
    public int LineCount { get; } = recordCount;
    public string Key { get; } = key;
    public bool Valid { get; } = valid;
    public string? ErrorMessage { get; } = errorMessage;
}