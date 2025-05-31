
using Microsoft.Extensions.Logging;

public class HeaderValidator : IHeaderValidator
{
    private readonly ILogger<HeaderValidator> _logger;

    public HeaderValidator(ILogger<HeaderValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates the headers of the file based on the provided file configuration.
    /// This method is called when the file is being processed.
    /// It checks if the headers match the expected format and validates each field
    /// </summary>
    /// <param name="lineCount"></param>
    /// <param name="line"></param>
    /// <param name="fileConfig"></param>
    /// <returns></returns>
    public List<RecordResult> Validate(int lineCount, string line, FileConfig fileConfig)
    {
        var results = new List<RecordResult>();
        var fields = line.Split(fileConfig.Delimiter);

        // Check for missing or extra columns in header
        ScanForNullOrExtraColumns(lineCount, fileConfig, results, fields, fileConfig.ValidationConfigs.Count);

        // Loop and invoke the validation for each field
        for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
        {
            var field = fields[fieldIndex];
            var validatorByName = fileConfig.ValidationConfigs.FirstOrDefault(
                    v => v.Name.Equals(field, StringComparison.OrdinalIgnoreCase));
            if (validatorByName == null)
            {
                // If the field is not found in the validation configs, add an error result
                results.Add(new RecordResult(lineCount, field, false, $"Header field '{field}' not found in validation config"));
                continue;
            }
        }
        return results;
    }

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
    private static void ScanForNullOrExtraColumns(int lineCount, FileConfig fileConfig, List<RecordResult> results, string[] fields, int expectedCount)
    {
        if (fields.Length < expectedCount)
        {
            results.Add(new RecordResult(lineCount, string.Join(fileConfig.Delimiter, fields), false, $"Header is missing {expectedCount - fields.Length} column(s)"));
        }
        else
        {
            results.Add(new RecordResult(lineCount, string.Join(fileConfig.Delimiter, fields), false, $"Header has {fields.Length - expectedCount} extra column(s)"));
        }
    }

}