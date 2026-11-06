using validator.Models;

namespace validator.Headless;

/// <summary>
/// JSON contract emitted by <c>validator --spec &lt;type&gt; --file &lt;path&gt;</c>.
/// Serialized with camelCase property names. Bump <see cref="CurrentSchemaVersion"/>
/// whenever a breaking change is made so frontends can detect incompatibilities.
/// </summary>
public sealed class ValidationReport
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? Spec { get; init; }
    public string? FilePath { get; init; }
    public long FileSizeBytes { get; init; }
    public long DurationMs { get; init; }
    public ReportStats? Stats { get; init; }
    public Summary? Summary { get; init; }

    /// <summary>
    /// Every line result produced by the engine, including the header line entry when the
    /// specification declares <c>headerLine: true</c> (mirrors the JSON writer behaviour).
    /// </summary>
    public List<LineResult> Lines { get; init; } = [];
}

/// <summary>
/// Pre-computed statistics so that frontends don't need to re-derive them from <see cref="ValidationReport.Lines"/>.
/// </summary>
public sealed class ReportStats
{
    public int TotalLines { get; init; }
    public int DataLines { get; init; }
    public int ValidLines { get; init; }
    public int InvalidLines { get; init; }
    public int FieldErrors { get; init; }
    public bool HeaderValid { get; init; }
    public double PassRate { get; init; }

    /// <summary>Field error counts keyed by field name (or <c>"(line)"</c> for line-level issues).</summary>
    public Dictionary<string, int> ErrorsByField { get; init; } = [];

    public static ReportStats From(IReadOnlyList<LineResult> lines, bool headerLine)
    {
        var dataLines = headerLine && lines.Count > 0 ? lines.Skip(1).ToList() : lines.ToList();
        var validData = dataLines.Count(l => l.Valid);
        var failedRecords = lines
            .SelectMany(l => l.RecordResults ?? Enumerable.Empty<RecordResult>())
            .Where(r => !r.Valid)
            .ToList();

        var byField = failedRecords
            .GroupBy(r => string.IsNullOrWhiteSpace(r.FieldName) ? "(line)" : r.FieldName!)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count());

        return new ReportStats
        {
            TotalLines = lines.Count,
            DataLines = dataLines.Count,
            ValidLines = lines.Count(l => l.Valid),
            InvalidLines = lines.Count(l => !l.Valid),
            FieldErrors = failedRecords.Count,
            HeaderValid = !headerLine || (lines.Count > 0 && lines[0].Valid),
            PassRate = dataLines.Count == 0 ? 0d : Math.Round(validData * 100d / dataLines.Count, 2),
            ErrorsByField = byField
        };
    }
}

/// <summary>
/// JSON contract emitted by <c>validator --list-specs</c>.
/// </summary>
public sealed class SpecListReport
{
    public int SchemaVersion { get; init; } = ValidationReport.CurrentSchemaVersion;
    public string? SpecDirectory { get; init; }
    public List<SpecDescriptor> Specs { get; init; } = [];
}

public sealed class SpecDescriptor
{
    public required string FileType { get; init; }
    public required string Delimiter { get; init; }
    public bool HeaderLine { get; init; }
    public List<ValidationConfig> Columns { get; init; } = [];
}
