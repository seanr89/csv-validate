
public class LineResult(int lineCount, bool valid, string? message)
{
    public int LineCount { get; } = lineCount;
    public bool Valid { get; set; } = valid;
    public string? Message { get; set; } = message;
    public List<RecordResult>? RecordResults { get; set; } = [];

    /// <summary>
    /// simple add simple result for a record object
    /// </summary>
    /// <param name="recordResult"></param>
    public void AddRecordResult(RecordResult? recordResult)
    {
        if(recordResult == null)
        {
            return;
        }
        RecordResults ??= new List<RecordResult>();
        RecordResults.Add(recordResult);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="recordResults"></param>
    public void AddRecordResults(List<RecordResult> recordResults)
    {
        if (RecordResults == null)
        {
            RecordResults = new List<RecordResult>();
        }
        RecordResults.AddRange(recordResults);
    }
}