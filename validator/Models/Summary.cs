
public class Summary
{
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public string FileName { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.Now;

    public Summary(int totalRecords, int validRecords, int invalidRecords, string fileName)
    {
        TotalRecords = totalRecords;
        ValidRecords = validRecords;
        InvalidRecords = invalidRecords;
        FileName = fileName;
    }

    public override string ToString()
    {
        return @$"Total Records: {TotalRecords}, Valid Records: {ValidRecords}, 
            Invalid Records: {InvalidRecords}, File Name: {FileName}, Processed At: {ProcessedAt}";
    }
}