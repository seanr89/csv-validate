using validator.Models;
using validator.Services.Interfaces;
using validator.Services.Rules;

namespace validator.Services;

public class ValidatorService : IValidatorService
{
    private readonly ITypeValidator _typeValidator;
    private readonly IHeaderValidator _headerValidator;
    private readonly IReadOnlyList<IFieldRule> _fieldRules;

    public ValidatorService(ITypeValidator typeValidator, IHeaderValidator headerValidator)
        : this(typeValidator, headerValidator, null)
    {
    }

    public ValidatorService(ITypeValidator typeValidator, IHeaderValidator headerValidator, IEnumerable<IFieldRule>? customRules)
    {
        _headerValidator = headerValidator;
        _typeValidator = typeValidator;
        _fieldRules = customRules?.ToList() ??
        [
            new RequiredFieldRule(),
            new LengthFieldRule(),
            new AllowedValuesFieldRule(),
            new TypeFieldRule(_typeValidator)
        ];
    }

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
            PopulateSummary(summary, filePath, results);
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

            // Blank lines (e.g. a trailing newline at the end of the file) carry no record
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            results.Add(ProcessLine(i + 1, lines[i], fileConfig));
        }

        PopulateSummary(summary, filePath, results);
        return results;
    }

    private static void PopulateSummary(Summary? summary, string filePath, List<LineResult> results)
    {
        if (summary == null)
        {
            return;
        }

        summary.FileName = Path.GetFileName(filePath.Replace('\\', '/'));
        summary.TotalRecords = results.Count;
        summary.ValidRecords = results.Count(r => r.Valid);
        summary.InvalidRecords = results.Count(r => !r.Valid);
        summary.ProcessedAt = DateTime.Now;
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
    /// Handle single line processing of a line.
    /// Evaluates rules per column and returns a line result.
    /// </summary>
    /// <param name="lineCount">current line increment count</param>
    /// <param name="line">file line record</param>
    /// <param name="fileConfig"></param>
    /// <returns></returns>
    private LineResult ProcessLine(int lineCount, string line, FileConfig fileConfig)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return new LineResult(lineCount, false, "No fields found");
        }

        var fields = line.Split(fileConfig.Delimiter);
        var result = new LineResult(lineCount, false, string.Empty);

        for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
        {
            var field = fields[fieldIndex];
            var validationConfig = fileConfig.ValidationConfigs.FirstOrDefault(v => v.Index == fieldIndex);
            if (validationConfig == null)
            {
                return new LineResult(lineCount, false, $"Field index {fieldIndex} not found in validation configs");
            }

            ValidateField(lineCount, fieldIndex, field, validationConfig, result);
        }

        // Flag missing columns on short rows
        foreach (var missingConfig in fileConfig.ValidationConfigs.Where(v => v.Index >= fields.Length))
        {
            result.AddRecordResult(new RecordResult(lineCount, missingConfig.Index, missingConfig.Name, string.Empty, false,
                RuleMessageFormatter.FormatErrorMessage(missingConfig.Index, missingConfig.Name, string.Empty,
                    $"Column is missing - row has {fields.Length} column(s) but the specification expects at least {missingConfig.Index + 1}", missingConfig.ErrorMessage)));
        }

        // The line is valid unless a field failed - passing results (e.g. empty nullable fields) don't count
        if (result.RecordResults == null || result.RecordResults.All(r => r.Valid))
        {
            result.Valid = true;
            result.Message = "Line is valid";
        }

        return result;
    }

    private void ValidateField(int lineCount, int fieldIndex, string field, ValidationConfig config, LineResult result)
    {
        foreach (var rule in _fieldRules)
        {
            var ruleResult = rule.Validate(lineCount, fieldIndex, field, config);
            if (ruleResult.RecordResult != null)
            {
                result.AddRecordResult(ruleResult.RecordResult);
            }

            if (!ruleResult.ShouldContinue)
            {
                break;
            }
        }
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