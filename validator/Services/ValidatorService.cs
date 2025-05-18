
public class ValidatorService : IValidatorService
{
    private readonly ITypeValidator _typeValidator;

    public ValidatorService(ITypeValidator typeValidator)
    {
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
    /// <returns></returns>
    public List<LineResult> ProcessFileWithConfig(string filePath, FileConfig fileConfig)
    {
        Console.WriteLine($"ValidatorService::ProcessFileWithConfig: {filePath}");
        // Load the file and read the lines all together
        var lines = File.ReadAllLines(filePath);
        List<LineResult> results = [];

        // Run the file through the validator for header lines if needed
        if (fileConfig.HeaderLine)
        {
            // TODO: if header process is there we should also check the line config to the header positioning!
            // Process the header line
            var line = lines[0];
            var lineCount = 1; // Line numbers are 1-based
            // Validate the field based on the validation config
            // Shift to a dedicated function here!
            var result = new LineResult(lineCount, false, string.Empty);
            (bool _, LineResult? value) = TryProcessHeader(lineCount, line, fileConfig, ref result);
            results.Add(result);
        }

        // Process each line one at a time!
        for (int i = 0; i < lines.Length; i++)
        {
            if (i == 0 && fileConfig.HeaderLine)
            {
                // Skip the header line
                continue;
            }
            results.Add(ProcessLine(i + 1, lines[i], fileConfig));
        }
        return results;
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

            // Now lets to null check?
            if (string.IsNullOrEmpty(field) && !validationConfig.IsNullable)
            {
                result.AddRecordResult(new RecordResult(lineCount, field, false, validationConfig.ErrorMessage));
                continue;
            }

            // check for null with allowance and skip if needed
            if (string.IsNullOrEmpty(field) && validationConfig.IsNullable)
            {
                result.AddRecordResult(new RecordResult(lineCount, field, true, string.Empty));
                continue;
            }

            // minlength and max length checks
            if (validationConfig?.minLength != null && field.Length < validationConfig.minLength)
            {
                result.AddRecordResult(new RecordResult(lineCount, field, false, validationConfig.ErrorMessage));
                continue;
            }
            if (validationConfig?.maxLength != null && field.Length > validationConfig.maxLength)
            {
                result.AddRecordResult(new RecordResult(lineCount, field, false, validationConfig.ErrorMessage));
                continue;
            }

            // This is very messy and should be moved to a dedicated function
            // expected values checks - if expected values are set
            // we need to check if the field is in the expected values
            if ((validationConfig?.HasExpected ?? false != true) && !validationConfig.AllowedValues.Contains(field.Trim()))
            {
                result.AddRecordResult(new RecordResult(lineCount, field, false, validationConfig.ErrorMessage));
                continue;
            }

            // move validationConfig type check to dedicated function etc...
            result.AddRecordResult(ProcessFieldByType(lineCount, field, validationConfig, fieldIndex));
        }//End of for loop

        // If we get here without any results, we are valid
        if(result.RecordResults == null || result.RecordResults.Count == 0)
        {
            result.Valid = true;
            result.Message = "Line is valid";
        }

        return result;
    }

    /// <summary>
    /// Process the field by type (date, int, decimal, etc...)
    /// This will check the field against the validation config type
    /// and return a result
    /// </summary>
    /// <param name="lineCount">current line location!</param>
    /// <param name="field"></param>
    /// <param name="validationConfig"></param>
    /// <param name="fieldIndex"></param>
    /// <returns></returns>
    RecordResult? ProcessFieldByType(int lineCount, string field, ValidationConfig validationConfig, int fieldIndex)
    {
        var typeResult = _typeValidator.ValidateType(field, validationConfig.type, validationConfig.Formats);

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
    /// <param name="flowControl"></param>
    /// <param name="lineCount"></param>
    /// <param name="line"></param>
    /// <param name="fileConfig"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    private (bool flowControl, LineResult? value) TryProcessHeader(int lineCount, string line, FileConfig fileConfig, ref LineResult result)
    {
        // Validate the field based on the validation config
        // Shift to a dedicated function here!
        if (fileConfig.HeaderLine)
        {
            var headerResult = validateHeaders(lineCount, line, fileConfig);
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

        return (flowControl: true, value: null);
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
    List<RecordResult> validateHeaders(int lineCount, string line, FileConfig fileConfig)
    {
        //Console.WriteLine($"ValidatorService::validateHeaders: {lineCount}");
        var results = new List<RecordResult>();
        var fields = line.Split(fileConfig.Delimiter);

        // here we now want to invoke the validation for each field
        for(int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
        {
            var field = fields[fieldIndex];
            var validatorByName = fileConfig.ValidationConfigs.FirstOrDefault(
                    v => v.Name.Equals(field, StringComparison.OrdinalIgnoreCase));
            if (validatorByName == null)
            {
                //Console.WriteLine($"ValidatorService::validateHeaders: Field {field} not found in validation configs");
                // If the field is not found in the validation configs, add an error result
                results.Add(new RecordResult(lineCount, field, false, $"Header field '{field}' not found in validation configs"));
                continue;
            }
        }
        return results;
    }
}