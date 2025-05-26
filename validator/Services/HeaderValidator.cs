
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
        var expectedCount = fileConfig.ValidationConfigs.Count;
        if (fields.Length != expectedCount)
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
        // here we now want to invoke the validation for each field
        for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
        {
            var field = fields[fieldIndex];
            var validatorByName = fileConfig.ValidationConfigs.FirstOrDefault(
                    v => v.Name.Equals(field, StringComparison.OrdinalIgnoreCase));
            if (validatorByName == null)
            {
                // If the field is not found in the validation configs, add an error result
                results.Add(new RecordResult(lineCount, field, false, $"Header field '{field}' not found in validation configs"));
                continue;
            }
        }
        return results;
    }
}