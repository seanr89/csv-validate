
public class ValidatorService
{
    public ValidatorService()
    {
        
    }

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
        //lines = lines.Take(50).ToArray(); // Limit to 50 lines for testing

        //TODO: move this to a function and before the for loop!!
        if(fileConfig.HeaderLine)
        {
            //Console.WriteLine($"ValidatorService::ProcessFileWithConfig: {filePath} has a header line");
            // Process the header line
            var line = lines[0];
            var lineCount = 1; // Line numbers are 1-based
            // Validate the field based on the validation config
            // Shift to a dedicated function here!
            var result = new LineResult(lineCount, false, string.Empty);
            (bool _, LineResult value) = TryProcessHeader(lineCount, line, fileConfig, ref result);
            results.Add(result);
        }

        // Process each line one at a time!
        for(int i = 0; i < lines.Length; i++)
        {
            if(i == 0 && fileConfig.HeaderLine)
            {
                // Skip the header line
                continue;
            }
            var line = lines[i];
            var lineCount = i + 1; // Line numbers are 1-based
            results.Add(ProcessLine(lineCount, line, fileConfig));
        }

        return results;
    }

    /// <summary>
    /// Handle single live processing of a line
    /// This will process the line and return a result
    /// </summary>
    /// <param name="lineCount"></param>
    /// <param name="line"></param>
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

        // //TODO: move this to a function and before the for loop!!
        // if(lineCount == 1)
        // {
        //     (bool flowControl, LineResult value) = TryProcessHeader(lineCount, line, fileConfig, ref result);
        //     if (!flowControl)
        //     {
        //         return value;
        //     }
        // }

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
            // expected values checks
            if (validationConfig.HasExpected && !validationConfig.AllowedValues.Contains(field))
            {
                result.AddRecordResult(new RecordResult(lineCount, field, false, validationConfig.ErrorMessage));
                continue;
            }

            // move validationConfig type checl to dedicated function
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

    RecordResult? ProcessFieldByType(int lineCount, string field, ValidationConfig validationConfig, int fieldIndex)
    {
        //var result = new RecordResult(lineCount, field, false, string.Empty);
        if(validationConfig.type == "date")
        {
            // Check if the field is a valid date
            if (!DateTime.TryParse(field, out DateTime dateValue))
            {
                Console.WriteLine($"ValidatorService::ProcessLine: {lineCount} - {field} field {fieldIndex} is not a date");
                return new RecordResult(lineCount, field, false, validationConfig.ErrorMessage);
            }
        }
        else if(validationConfig.type == "int")
        {
            // Check if the field is a valid int
            if (!int.TryParse(field, out int intValue))
            {
                return new RecordResult(lineCount, field, false, validationConfig.ErrorMessage);
            }
        }
        else if(validationConfig.type == "decimal")
        {
            // Check if the field is a valid decimal
            if (!decimal.TryParse(field, out decimal decimalValue))
            {
                return new RecordResult(lineCount, field, false, validationConfig.ErrorMessage);
            }
        }
        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="flowControl"></param>
    /// <param name="lineCount"></param>
    /// <param name="line"></param>
    /// <param name="fileConfig"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    private (bool flowControl, LineResult value) TryProcessHeader(int lineCount, string line, FileConfig fileConfig, ref LineResult result)
    {
        //Console.WriteLine($"ValidatorService::TryProcessHeader: {lineCount}");
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
            // Console.WriteLine($"Validating header field {fieldIndex}: {fields[fieldIndex]}");
            // Console.WriteLine($"Validating header field {fieldIndex}: {fileConfig.ValidationConfigs[fieldIndex].Name}");
            var field = fields[fieldIndex];
            var validatorByName = fileConfig.ValidationConfigs.FirstOrDefault(
                    v => v.Name.Equals(field, StringComparison.OrdinalIgnoreCase));
            if (validatorByName == null)
            {
                Console.WriteLine($"ValidatorService::validateHeaders: Field {field} not found in validation configs");
                // If the field is not found in the validation configs, add an error result
                results.Add(new RecordResult(lineCount, field, false, $"Header field '{field}' not found in validation configs"));
                continue;
            }
        }
        return results;
    }
}