
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
        // Load the file
        var lines = File.ReadAllLines(filePath);
        var results = new List<LineResult>();

        // Process each line
        for(int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineCount = i + 1; // Line numbers are 1-based
            results.Add(ProcessLine(lineCount, line, fileConfig));
        }

        //TODO:
        if(results.Any(r => r.Valid == false) == true)
        {
            Console.WriteLine($"ValidatorService::ProcessFileWithConfig: {filePath} has errors");
        }

        return results;
    }

    /// <summary>
    /// TODO: handle single file flow
    /// </summary>
    /// <param name="lineCount"></param>
    /// <param name="line"></param>
    /// <param name="fileConfig"></param>
    /// <returns></returns>
    private LineResult ProcessLine(int lineCount, string line, FileConfig fileConfig)
    {
        var result = new LineResult(lineCount, false, string.Empty);
        // Split the line up properly
        var fields = line.Split(fileConfig.Delimiter);
        (bool flowControl, LineResult value) = TryProcessHeader(lineCount, line, fileConfig, ref result);
        if (!flowControl)
        {
            return value;
        }

        // Initialise new array of record results??
        List<RecordResult> recordResults = new List<RecordResult>();

        // here we now want to invoke the validation for each field
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
                result = new LineResult(lineCount, false, $"Field {fieldIndex} is null or empty");
                return result;
            }
            if (field.Length < validationConfig.minLength)
            {
                result = new LineResult(lineCount, false, $"Field {fieldIndex} is too short");
                return result;
            }
            if (field.Length > validationConfig.maxLength)
            {
                result = new LineResult(lineCount, false, $"Field {fieldIndex} is too long");
                return result;
            }
            if (validationConfig.HasExpected && !validationConfig.AllowedValues.Contains(field))
            {
                result = new LineResult(lineCount, false, $"Field {fieldIndex} has unexpected value");
                return result;
            }
        }

        return result;
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
        // Validate the field based on the validation config
        // Shift to a dedicated function here!
        if (lineCount == 0 && fileConfig.HeaderLine)
        {
            // Skip header line
            var headerResult = validateHeaders(lineCount, line, fileConfig);
            if (headerResult.Count > 0)
            {
                // result = new LineResult(lineCount, false, string.Join(", ", headerResult.Select(r => r.ErrorMessage)));
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
        var results = new List<RecordResult>();
        var fields = line.Split(fileConfig.Delimiter);

        // here we now want to invoke the validation for each field
        for(int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
        {
            Console.WriteLine($"Validating header field {fieldIndex}: {fields[fieldIndex]}");
            Console.WriteLine($"Validating header field {fieldIndex}: {fileConfig.ValidationConfigs[fieldIndex].Name}");
            var field = fields[fieldIndex];
            var validatorByName = fileConfig.ValidationConfigs.FirstOrDefault(
                    v => v.Name.Equals(field, StringComparison.OrdinalIgnoreCase));
            if (validatorByName == null)
            {
                // If the field is not found in the validation configs, add an error result
                results.Add(new RecordResult(lineCount, field, false, $"Header field '{field}' not found in validation configs"));
                continue;
            }
            else
            {
                //TODO
            }
        }

        return results;
    }
}