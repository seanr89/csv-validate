
public record LineResult(int lineCount, bool valid, string? errorMessage)
{
    public int LineCount { get; } = lineCount;
    public bool Valid { get; } = valid;
    public string? ErrorMessage { get; } = errorMessage;
    public List<RecordResult>? RecordResults { get; set; } = null;

    // public override string ToString()
    // {
    //     return $"LineCount: {LineCount}, Valid: {Valid}, ErrorMessage: {ErrorMessage}";
    // }

    public void AddRecordResult(RecordResult recordResult)
    {
        if (RecordResults == null)
        {
            RecordResults = new List<RecordResult>();
        }
        RecordResults.Add(recordResult);
    }

    public void AddRecordResults(List<RecordResult> recordResults)
    {
        if (RecordResults == null)
        {
            RecordResults = new List<RecordResult>();
        }
        RecordResults.AddRange(recordResults);
    }
}