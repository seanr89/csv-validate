
public class Summary
{
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public List<string> ErrorMessages { get; set; } = new List<string>();

    public Summary(int totalRecords, int validRecords, int invalidRecords, List<string> errorMessages)
    {
        TotalRecords = totalRecords;
        ValidRecords = validRecords;
        InvalidRecords = invalidRecords;
        ErrorMessages = errorMessages;
    }

    public override string ToString()
    {
        return $"Total Records: {TotalRecords}, Valid Records: {ValidRecords}, Invalid Records: {InvalidRecords}, Errors: {string.Join(", ", ErrorMessages)}";
    }
}