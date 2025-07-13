
namespace validator.Models;

/// <summary>
/// A summary model so that an export can be returned at end of run
/// </summary>
public class Summary(int totalRecords, int validRecords, int invalidRecords, string fileName)
{
    public int TotalRecords { get; set; } = totalRecords;
    public int ValidRecords { get; set; } = validRecords;
    public int InvalidRecords { get; set; } = invalidRecords;
    public string FileName { get; set; } = fileName;
    public DateTime ProcessedAt { get; set; } = DateTime.Now;

    public override string ToString()
    {
        return @$"Total Records: {TotalRecords}, Valid Records: {ValidRecords}, 
            Invalid Records: {InvalidRecords}, File Name: {FileName}, Processed At: {ProcessedAt}";
    }
}