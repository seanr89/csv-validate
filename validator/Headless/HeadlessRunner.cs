using System.Diagnostics;
using validator.Models;
using validator.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace validator.Headless;

/// <summary>
/// Non-interactive entry point used by external frontends (e.g. the Rust desktop client).
/// Emits a single JSON document to the supplied output writer (or <c>--output</c> file) and
/// returns a process exit code. All diagnostic chatter is expected to be routed to stderr by the caller.
/// </summary>
public sealed class HeadlessRunner
{
    public const int ExitValid = 0;
    public const int ExitInvalid = 1;
    public const int ExitError = 2;

    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        // camelCase property names, but leave dictionary keys (field names) untouched.
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = false }
        },
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Include,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        Converters = { new StringEnumConverter() }
    };

    private readonly IValidatorService _validatorService;
    private readonly ISpecificationSelector _specificationSelector;
    private readonly TextWriter _output;
    private readonly TextWriter _diagnostics;
    private readonly string? _specDirectory;

    public HeadlessRunner(
        IValidatorService validatorService,
        ISpecificationSelector specificationSelector,
        TextWriter output,
        TextWriter diagnostics,
        string? specDirectory = null)
    {
        _validatorService = validatorService;
        _specificationSelector = specificationSelector;
        _output = output;
        _diagnostics = diagnostics;
        _specDirectory = specDirectory;
    }

    /// <summary>
    /// Executes the parsed headless command and returns the process exit code.
    /// </summary>
    public int Run(HeadlessOptions options)
    {
        if (options.Command == HeadlessCommand.Help)
        {
            if (options.Error != null)
            {
                _diagnostics.WriteLine($"Error: {options.Error}");
            }
            _diagnostics.WriteLine(HeadlessOptions.Usage);
            return options.Error == null ? ExitValid : ExitError;
        }

        if (options.Error != null)
        {
            return Emit(options, Failure(options, options.Error), ExitError);
        }

        return options.Command switch
        {
            HeadlessCommand.ListSpecs => ListSpecs(options),
            HeadlessCommand.Validate => Validate(options),
            _ => ExitError
        };
    }

    private int ListSpecs(HeadlessOptions options)
    {
        var specs = _specificationSelector.GetAvailableFileTypes()
            .Select(type => _specificationSelector.GetFileConfig(type))
            .Where(config => config != null)
            .Select(config => new SpecDescriptor
            {
                FileType = config!.FileType,
                Delimiter = config.Delimiter,
                HeaderLine = config.HeaderLine,
                Columns = config.ValidationConfigs.OrderBy(c => c.Index).ToList()
            })
            .OrderBy(s => s.FileType, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Emit(options, new SpecListReport { SpecDirectory = _specDirectory, Specs = specs }, ExitValid);
    }

    private int Validate(HeadlessOptions options)
    {
        var filePath = Path.GetFullPath(options.FilePath!);
        if (!File.Exists(filePath))
        {
            return Emit(options, Failure(options, $"File not found: {filePath}"), ExitError);
        }

        var config = _specificationSelector.GetFileConfig(options.Spec!);
        if (config == null)
        {
            var available = string.Join(", ", _specificationSelector.GetAvailableFileTypes());
            return Emit(options, Failure(options, $"Unknown specification '{options.Spec}'. Available: {available}"), ExitError);
        }

        if (config.ValidationConfigs.Count == 0)
        {
            return Emit(options, Failure(options, $"Specification '{options.Spec}' has no column definitions."), ExitError);
        }

        try
        {
            var summary = new Summary(0, 0, 0, Path.GetFileName(filePath));
            var stopwatch = Stopwatch.StartNew();
            var lines = _validatorService.ProcessFileWithConfig(filePath, config, summary) ?? [];
            stopwatch.Stop();

            var stats = ReportStats.From(lines, config.HeaderLine);
            var report = new ValidationReport
            {
                Success = true,
                Spec = config.FileType,
                FilePath = filePath,
                FileSizeBytes = new FileInfo(filePath).Length,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Stats = stats,
                Summary = summary,
                Lines = lines
            };

            return Emit(options, report, stats.InvalidLines > 0 ? ExitInvalid : ExitValid);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Emit(options, Failure(options, $"Unable to read file: {ex.Message}"), ExitError);
        }
    }

    private static ValidationReport Failure(HeadlessOptions options, string error) => new()
    {
        Success = false,
        Error = error,
        Spec = options.Spec,
        FilePath = options.FilePath
    };

    private int Emit(HeadlessOptions options, object payload, int exitCode)
    {
        var json = Serialize(payload);
        if (!string.IsNullOrWhiteSpace(options.OutputPath))
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(options.OutputPath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(options.OutputPath, json);
        }
        else
        {
            _output.WriteLine(json);
            _output.Flush();
        }

        if (payload is ValidationReport { Success: false, Error: { } error })
        {
            _diagnostics.WriteLine($"Error: {error}");
        }

        return exitCode;
    }

    /// <summary>
    /// Serializes a payload using the headless JSON contract settings (camelCase, indented).
    /// </summary>
    public static string Serialize(object payload) => JsonConvert.SerializeObject(payload, SerializerSettings);
}
