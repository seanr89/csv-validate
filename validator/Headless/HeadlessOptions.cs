namespace validator.Headless;

/// <summary>
/// The command requested by a non-interactive (headless) invocation of the validator.
/// </summary>
public enum HeadlessCommand
{
    /// <summary>No headless flags were supplied - fall back to the interactive Spectre.Console workflow.</summary>
    None,
    /// <summary>Emit the list of available specifications (with column definitions) as JSON.</summary>
    ListSpecs,
    /// <summary>Validate a single file against a named specification and emit a JSON report.</summary>
    Validate,
    /// <summary>Print headless usage information.</summary>
    Help
}

/// <summary>
/// Parsed non-interactive command line options. This is the contract used by external
/// frontends (e.g. the Rust desktop client in <c>client/</c>) to drive the validation engine.
/// </summary>
/// <remarks>
/// Supported flags:
/// <list type="bullet">
/// <item><c>--list-specs</c> - list available specifications as JSON.</item>
/// <item><c>--spec &lt;fileType&gt; --file &lt;path&gt;</c> - validate a file and emit a JSON report.</item>
/// <item><c>--spec-dir &lt;dir&gt;</c> - optional override for the Specifications directory.</item>
/// <item><c>--output &lt;path&gt;</c> - optional path to write the JSON payload to instead of stdout.</item>
/// <item><c>--help-headless</c> - print usage.</item>
/// </list>
/// </remarks>
public sealed class HeadlessOptions
{
    public HeadlessCommand Command { get; private set; } = HeadlessCommand.None;
    public string? Spec { get; private set; }
    public string? FilePath { get; private set; }
    public string? SpecDirectory { get; private set; }
    public string? OutputPath { get; private set; }

    /// <summary>
    /// Parse error message, if the supplied arguments were invalid. When set, <see cref="Command"/>
    /// still reflects the requested command so callers can report a usage error.
    /// </summary>
    public string? Error { get; private set; }

    public bool IsHeadless => Command != HeadlessCommand.None;

    public const string Usage =
        "Headless usage:\n" +
        "  validator --list-specs [--spec-dir <dir>] [--output <path>]\n" +
        "  validator --spec <fileType> --file <path> [--spec-dir <dir>] [--output <path>]\n" +
        "\n" +
        "Exit codes: 0 = all lines valid, 1 = validation errors found, 2 = usage or runtime error.";

    /// <summary>
    /// Parses the raw process arguments. Unknown arguments are ignored so that
    /// Generic Host style arguments (e.g. <c>--environment</c>) continue to work.
    /// </summary>
    public static HeadlessOptions Parse(string[]? args)
    {
        var options = new HeadlessOptions();
        if (args == null || args.Length == 0)
        {
            return options;
        }

        bool listSpecs = false;
        bool help = false;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--list-specs":
                    listSpecs = true;
                    break;
                case "--help-headless":
                    help = true;
                    break;
                case "--spec":
                    options.Spec = ReadValue(args, ref i, arg, options);
                    break;
                case "--file":
                    options.FilePath = ReadValue(args, ref i, arg, options);
                    break;
                case "--spec-dir":
                    options.SpecDirectory = ReadValue(args, ref i, arg, options);
                    break;
                case "--output":
                    options.OutputPath = ReadValue(args, ref i, arg, options);
                    break;
            }
        }

        if (help)
        {
            options.Command = HeadlessCommand.Help;
        }
        else if (listSpecs)
        {
            options.Command = HeadlessCommand.ListSpecs;
        }
        else if (options.Spec != null || options.FilePath != null)
        {
            options.Command = HeadlessCommand.Validate;
            if (options.Error == null && string.IsNullOrWhiteSpace(options.Spec))
            {
                options.Error = "Missing required argument '--spec <fileType>'.";
            }
            else if (options.Error == null && string.IsNullOrWhiteSpace(options.FilePath))
            {
                options.Error = "Missing required argument '--file <path>'.";
            }
        }
        else if (options.SpecDirectory != null || options.OutputPath != null)
        {
            // Modifier flags without a command are a usage error rather than a silent interactive launch.
            options.Command = HeadlessCommand.Help;
            options.Error ??= "No headless command supplied. Use '--list-specs' or '--spec <fileType> --file <path>'.";
        }

        return options;
    }

    private static string? ReadValue(string[] args, ref int i, string flag, HeadlessOptions options)
    {
        if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            options.Error ??= $"Missing value for '{flag}'.";
            return null;
        }

        i++;
        return args[i];
    }
}
