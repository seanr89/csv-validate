
public class LineResult(int lineCount, bool valid, string? message)
{
    public int LineCount { get; } = lineCount;
    public bool Valid { get; set; } = valid;
    public string? Message { get; set; } = message;
    public List<RecordResult>? RecordResults { get; set; } = [];

    /// <summary>
    /// Add single result to results array
    /// </summary>
    /// <param name="recordResult"></param>
    public void AddRecordResult(RecordResult? recordResult)
    {
        if(recordResult == null)
        {
            return;
        }
        RecordResults ??= [];
        RecordResults.Add(recordResult);
    }

    /// <summary>
    /// Allows bulk addition of results
    /// </summary>
    /// <param name="recordResults">expected at least 1 record in results</param>
    public void AddRecordResults(List<RecordResult> recordResults)
    {
        RecordResults ??= [];
        if(recordResults.Count > 0)
            RecordResults.AddRange(recordResults);
    }
}