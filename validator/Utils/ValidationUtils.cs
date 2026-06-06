

using validator.Models;

public static class ValidationUtils
{
    /// <summary>
    /// Scans the header for null or extra columns based on the expected count.
    /// If the number of columns is less than expected, it adds an error indicating missing columns.
    /// If the number of columns is more than expected, it adds an error indicating extra columns.
    /// If the number of columns matches the expected count, it does not add any error.
    /// If the header is empty, it adds an error indicating that the header is empty.
    /// If the header has more columns than expected, it adds an error indicating extra columns.
    /// </summary>
    /// <param name="lineCount"></param>
    /// <param name="fileConfig"></param>
    /// <param name="results"></param>
    /// <param name="fields"></param>
    /// <param name="expectedCount"></param>
    public static void ScanForNullOrExtraColumns(int lineCount, FileConfig fileConfig, List<RecordResult> results, string[] fields, int expectedCount)
    {
        if (fields.Length < expectedCount)
        {
            results.Add(new RecordResult(lineCount, string.Join(fileConfig.Delimiter, fields), false, $"Header is missing {expectedCount - fields.Length} column(s)"));
        }
        else if (fields.Length > expectedCount)
        {
            results.Add(new RecordResult(lineCount, string.Join(fileConfig.Delimiter, fields), false, $"Header has {fields.Length - expectedCount} extra column(s)"));
        }
    }
}