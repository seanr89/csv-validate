
using validator.Models;
using validator.Services.Interfaces;

namespace validator.Services;
public class HeaderValidator() : IHeaderValidator
{
    /// <summary>
    /// Validates the headers of the file based on the provided file configuration.
    /// This method is called when the file is being processed.
    /// It checks if the headers match the expected format and validates each field
    /// </summary>
    /// <param name="lineCount">current line increment being scanned</param>
    /// <param name="line">line string data</param>
    /// <param name="fileConfig">overall file configuration</param>
    /// <returns></returns>
    public List<RecordResult> Validate(int lineCount, string line, FileConfig fileConfig)
    {
        var results = new List<RecordResult>();
        var fields = line.Split(fileConfig.Delimiter);

        // Check for missing or extra columns in header
        ValidationUtils.ScanForNullOrExtraColumns(lineCount, fileConfig, results, fields, fileConfig.ValidationConfigs.Count);

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

    

}