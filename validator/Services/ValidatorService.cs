using validator.Models;
using validator.Services.Interfaces;

namespace validator.Services;

public class ValidatorService : IValidatorService
{
    private readonly ITypeValidator _typeValidator;
    private readonly IHeaderValidator _headerValidator;

    public ValidatorService(ITypeValidator typeValidator, IHeaderValidator headerValidator)
    {
        _headerValidator = headerValidator;
        // Inject the type validator to handle type-specific validations
        // This will be used to validate the field types based on the validation config
        _typeValidator = typeValidator;
    }

    /// <summary>
    /// Simple job to intake a file path and a file config for processing
    /// This will process the file and return a list of results
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="fileConfig"></param>
    /// <returns></returns>
    /// <summary>
    /// Simple job to intake a file path and a file config for processing
    /// This will process the file and return a list of results
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="fileConfig"></param>
    /// <param name="summary"></param>
    /// <returns></returns>
    public List<LineResult> ProcessFileWithConfig(string filePath, FileConfig fileConfig, Summary summary)
    {
        Console.WriteLine($"ValidatorService::ProcessFileWithConfig: {filePath}");
        // Load the file and read the lines all together
        var lines = File.ReadAllLines(filePath);
        List<LineResult> results = [];

        // Handle empty file
        if (lines.Length == 0)
        {
            results.Add(new LineResult(0, false, "File is empty"));
            return results;
        }

        // Run the file through the validator for header lines if needed
        if (fileConfig.HeaderLine)
        {
            // TODO: if header process is there we should also check the line config to the header positioning!
            var lineCount = 1; // Line numbers are 1-based
            var result = new LineResult(lineCount, false, string.Empty);
            (bool _, LineResult? value) = TryProcessHeader(lineCount, lines[0], fileConfig, ref result);
            results.Add(result);
        }

        // Process each line one at a time!
        for (int i = 0; i < lines.Length; i++)
        {
            // If the line should not be processed (i.e., it's a header), skip it.
            if (!ShouldProcessLine(fileConfig, i))
                continue;
            
            results.Add(ProcessLine(i + 1, lines[i], fileConfig));
        }
        return results;
    }
    
    /// <summary>
    /// Determines if a line at a given index should be processed as a data row.
    /// Skips the first line if it is configured as a header.
    /// </summary>
    /// <param name="fileConfig">The file configuration.</param>
    /// <param name="lineIndex">The zero-based index of the line.</param>
    /// <returns>False if the line is a header and should be skipped, otherwise true.</returns>
    private static bool ShouldProcessLine(FileConfig fileConfig, int lineIndex)
    {
        // It's a header line if it's the first line (index 0) and HeaderLine is true.
        bool isHeader = lineIndex == 0 && fileConfig.HeaderLine;
        // Process the line only if it's NOT a header.
        return !isHeader;
    }

    /// <summary>
    /// Handle single live processing of a line
    /// This will process the line and return a result
    /// </summary>
    /// <param name="lineCount">current line increment count</param>
    /// <param name="line">file line record!</param>
    /// <param name="fileConfig"></param>
    /// <returns></returns>
    private LineResult ProcessLine(int lineCount, string line, FileConfig fileConfig)
    {
        var result = new LineResult(lineCount, false, string.Empty);
        // Split the line up properly by the delimiter
        var fields = line.Split(fileConfig.Delimiter);
        //Check for empty lines?
        if(fields.Length <= 0)
        {
            //Console.WriteLine($"ValidatorService::ProcessLine: {lineCount} - no fields");
            result = new LineResult(lineCount, false, "No fields found");
            return result;
        }

        // here we now want to invoke the validation for each field step by step
        for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
        {
            // Pull the field and the expected validation config out!
            var field = fields[fieldIndex];
            var validationConfig = fileConfig.ValidationConfigs.FirstOrDefault(
                    v => v.Index == fieldIndex);
            if (validationConfig == null)
            {
                // If the field is not found in the validation configs, add an error result
                result = new LineResult(lineCount, false, $"Field index {fieldIndex} not found in validation configs");
                return result;
            }
            var activeValidationConfig = validationConfig;

            // Now lets to null check?
            if (string.IsNullOrEmpty(field) && !activeValidationConfig.IsNullable)
            {
                result.AddRecordResult(new RecordResult(lineCount, field, false, activeValidationConfig.ErrorMessage));
                continue;
            }

            // check for null with allowance and skip if needed
            if (string.IsNullOrEmpty(field) && activeValidationConfig.IsNullable)
            {
                result.AddRecordResult(new RecordResult(lineCount, field, true, string.Empty));
                continue;
            }

            bool flowControl = ProcessMinandMaxLengths(lineCount, result, field, activeValidationConfig);
            if (!flowControl)
            {
                continue;
            }

            // This is very messy and should be moved to a dedicated function
            // expected values checks - if expected values are set
            // we need to check if the field is in the expected values
            if ((activeValidationConfig.HasExpected != true) &&
                activeValidationConfig.AllowedValues != null
                    && !activeValidationConfig.AllowedValues.Contains(field.Trim()))
            {
                result.AddRecordResult(new RecordResult(lineCount, field, false, activeValidationConfig.ErrorMessage));
                continue;
            }

            // move validationConfig type check to dedicated function etc...
            result.AddRecordResult(ProcessFieldByType(lineCount, field, activeValidationConfig, fieldIndex));
        }//End of for loop

        // If we get here without any results, we are valid
        if (result.RecordResults == null || result.RecordResults.Count == 0)
        {
            result.Valid = true;
            result.Message = "Line is valid";
        }

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="lineCount"></param>
    /// <param name="result"></param>
    /// <param name="field"></param>
    /// <param name="activeValidationConfig"></param>
    /// <returns></returns>
    private static bool ProcessMinandMaxLengths(int lineCount, LineResult result, string field, ValidationConfig activeValidationConfig)
    {
        // minlength and max length checks
        if (activeValidationConfig.minLength != null && field.Length < activeValidationConfig.minLength)
        {
            result.AddRecordResult(new RecordResult(lineCount, field, false, activeValidationConfig.ErrorMessage));
            return false;
        }
        if (activeValidationConfig.maxLength != null && field.Length > activeValidationConfig.maxLength)
        {
            result.AddRecordResult(new RecordResult(lineCount, field, false, activeValidationConfig.ErrorMessage));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Process the field by type (date, int, decimal, etc...)
    /// This will check the field against the validation config type
    /// and return a result
    /// </summary>
    /// <param name="lineCount">current line location!</param>
    /// <param name="field">field param to be parsed and validated</param>
    /// <param name="validationConfig">the configuration file to be used to search</param>
    /// <param name="fieldIndex">unsure</param>
    /// <returns>nullable record result</returns>
    RecordResult? ProcessFieldByType(int lineCount, string field, ValidationConfig validationConfig, int fieldIndex)
    {
        // Consistent handling for null/empty fields
        if (string.IsNullOrEmpty(field))
        {
            if (validationConfig.IsNullable)
            {
                // Valid: nullable and empty
                return new RecordResult(lineCount, field, true, string.Empty);
            }
            // Invalid: not nullable and empty
            return new RecordResult(lineCount, field, false, validationConfig.ErrorMessage);
        }

        bool typeResult = _typeValidator.ValidateType(field, validationConfig.type, validationConfig.Formats);
        if (!typeResult)
        {
            return new RecordResult(lineCount, field, false, validationConfig.ErrorMessage);
        }
        return null;
    }

    /// <summary>
    /// Dedicated function to process the header line
    /// This will check the header line against the validation config
    /// and return a result
    /// </summary>
    /// <param name="lineCount">current line location!</param>
    /// <param name="line">line param to be parsed and validated</param>
    /// <param name="fileConfig">configuration element</param>
    /// <param name="result"/>ref param</param>
    /// <returns></returns>
    private (bool flowControl, LineResult? value) TryProcessHeader(int lineCount, string line, FileConfig fileConfig, ref LineResult result)
    {
        // Validate the field based on the validation config
        var headerResult = _headerValidator.Validate(lineCount, line, fileConfig);
        if (headerResult.Count > 0)
        {
            result = new LineResult(lineCount, false, "Header line is invalid");
            result.AddRecordResults(headerResult);
            return (flowControl: false, value: result);
        }
        else
        {
            result = new LineResult(lineCount, true, string.Empty);
        }
        return (flowControl: false, value: result);
    }
}