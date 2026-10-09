
namespace validator.Models;

public class RecordResult
{
    public int LineCount { get; }
    public int? FieldIndex { get; }
    public string? FieldName { get; }
    public string Key { get; }
    public bool Valid { get; }
    public string? ErrorMessage { get; }

    public RecordResult(
        int recordCount,
        string key,
        bool valid,
        string? errorMessage
    )
    {
        LineCount = recordCount;
        FieldIndex = null;
        FieldName = null;
        Key = key;
        Valid = valid;
        ErrorMessage = errorMessage;
    }

    public RecordResult(
        int recordCount,
        int? fieldIndex,
        string? fieldName,
        string key,
        bool valid,
        string? errorMessage
    )
    {
        LineCount = recordCount;
        FieldIndex = fieldIndex;
        FieldName = fieldName;
        Key = key;
        Valid = valid;
        ErrorMessage = errorMessage;
    }
}