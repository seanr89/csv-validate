using validator.Models;
using validator.Models.Settings;
using validator.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Spectre.Console;

namespace validator;

public class App
{
    private readonly ILogger<App> _logger;
    private readonly IValidatorService _validatorService;
    private readonly ISpecificationSelector _specificationSelector;
    private readonly RunSettings _runSettings;

    public App(
        ILogger<App> logger,
        IValidatorService validatorService,
        ISpecificationSelector specificationSelector,
        IOptions<RunSettings>? runSettingsOptions = null)
    {
        _logger = logger;
        _validatorService = validatorService;
        _specificationSelector = specificationSelector;
        _runSettings = runSettingsOptions?.Value ?? new RunSettings();
    }

    /// <summary>
    /// Entry point for application to execute job flow
    /// Steps:
    /// Request and validate the file request type!
    /// Load in all possible files by type!
    /// </summary>
    public void Run()
    {
        _logger.LogInformation("App::Run");

        string fileType = CreateAndWaitForResponse("Select file type", ["transactions", "account", "customer", "card"]);

        var fileConfig = _specificationSelector.GetFileConfig(fileType);
        if (fileConfig == null)
            return;

        if (fileConfig.ValidationConfigs.Count == 0)
        {
            Console.WriteLine($"Error: No validation configs found for {fileType} - ending program");
            return;
        }

        // Resolve input directory from RunSettings (InboxPrefix)
        var inputDir = ResolveDirectoryPath(_runSettings.InboxPrefix, "files", mustExist: true);
        if (!Directory.Exists(inputDir))
        {
            Console.WriteLine($"Error: Input directory {inputDir} does not exist.");
            return;
        }

        // go grab the files for the type
        var files = Directory.GetFiles(inputDir, $"{fileType}.csv");
        if (files.Length == 0)
        {
            Console.WriteLine($"Error: No files found for {fileType} in {inputDir}");
            return;
        }

        // Resolve output directory from RunSettings (OutboxPrefix)
        var outputDir = ResolveDirectoryPath(_runSettings.OutboxPrefix, "files/outputs", mustExist: false);

        // now we want to process each file
        foreach (var file in files)
        {
            Summary summary = new Summary(0, 0, 0, Path.GetFileName(file));
            var results = _validatorService.ProcessFileWithConfig(file, fileConfig, summary);
            if (results == null)
            {
                Console.WriteLine($"Error: Unable to process file {file}");
                continue;
            }
            else
            {
                var count = results.Count(r => r.Valid == false);
                if (count > 0)
                {
                    Console.WriteLine($"File {file} has {count} errors");
                }
            }
            // Ask if we want to write the results to a file via csv or json?
            if (AnsiConsole.Confirm("Do you want to write the results to file?"))
            {
                // ask for the file type
                var fileTypeToWrite = CreateAndWaitForResponse("Select file type to write", ["csv", "json"]);
                if (fileTypeToWrite == "csv")
                {
                    // if the file had a header - skip that line from the CSV records
                    var csvResults = fileConfig.HeaderLine && results.Count > 0 ? results.Skip(1) : results;

                    // write to csv (we need to grab the records from the results)
                    var records = csvResults.SelectMany(r => r.RecordResults ?? Enumerable.Empty<RecordResult>()).ToList();
                    FileWriter.TryWriteOrAppendToFile(records, $"{fileType}_results.csv", outputDirectory: outputDir);
                }
                else
                {
                    FileWriter.WriteToJson(file, results, outputDirectory: outputDir);
                }
            }

            // Print summary of results
            Console.WriteLine("Summary of results: {0}", summary.ToString());
        } // end of file loop

        _logger.LogInformation("App::Completed");
    }

    /// <summary>
    /// Resolves a directory path against the current working directory and AppContext.BaseDirectory.
    /// Supports running from validator/, the repository root, or test runners.
    /// </summary>
    public static string ResolveDirectoryPath(string? configuredPath, string defaultPath, bool mustExist = true)
    {
        var candidate = string.IsNullOrWhiteSpace(configuredPath) ? defaultPath : configuredPath;

        // 1. Direct match
        if (Directory.Exists(candidate))
        {
            return Path.GetFullPath(candidate);
        }

        // 2. Relative check adjusting for running from validator/ vs repository root
        if (candidate.StartsWith("../") && Directory.Exists(candidate[3..]))
        {
            return Path.GetFullPath(candidate[3..]);
        }

        if (!candidate.StartsWith("../") && Directory.Exists(Path.Combine("..", candidate)))
        {
            return Path.GetFullPath(Path.Combine("..", candidate));
        }

        // 3. Relative to AppContext.BaseDirectory
        var fromBase = Path.Combine(AppContext.BaseDirectory, candidate);
        if (Directory.Exists(fromBase))
        {
            return Path.GetFullPath(fromBase);
        }

        // 4. Upward search from AppContext.BaseDirectory for project root
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        var trimmed = candidate.TrimStart('.', '/', '\\');
        while (dir != null)
        {
            var testPath = Path.Combine(dir.FullName, trimmed);
            if (Directory.Exists(testPath))
            {
                return Path.GetFullPath(testPath);
            }
            dir = dir.Parent;
        }

        // For non-existing output directories, resolve parent if possible
        if (!mustExist)
        {
            if (!candidate.StartsWith("../") && Directory.Exists("../files"))
            {
                return Path.GetFullPath(Path.Combine("..", candidate));
            }
            return Path.GetFullPath(candidate);
        }

        return candidate;
    }

    

    /// <summary>
    /// Create selection prompt and wait for response
    /// Only a single choice is allowed!
    /// </summary>
    /// <param name="message">Question to be asked</param>
    /// <param name="choices">array or string option types</param>
    string CreateAndWaitForResponse(string message, string[] choices)
    {
        var response = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(message)
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more choices)[/]")
                .AddChoices(choices));

        // Echo the response back to the terminal
        AnsiConsole.MarkupLine($"You selected [red]{response}[/]!");

        return response;
    }
}