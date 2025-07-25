
using validator.Models;
using validator.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Spectre.Console;

namespace validator;

public class App
{
    private readonly ILogger<App> _logger;
    private readonly IValidatorService _validatorService;
    private readonly ISpecificationSelector _specificationSelector;

    public App(ILogger<App> logger, IValidatorService validatorService, ISpecificationSelector specificationSelector)
    {
        _logger = logger;
        _validatorService = validatorService;
        _specificationSelector = specificationSelector;
    }

    /// <summary>
    /// Entry point for application to execute jobs
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

        Console.WriteLine($"File config found for {fileType} with {fileConfig.ValidationConfigs.Count} line configs");

        // go grab the files for the type
        var files = Directory.GetFiles("../files/", $"{fileType}.csv");
        if (files.Length == 0)
        {
            Console.WriteLine($"Error: No files found for {fileType}");
            return;
        }

        AnsiConsole.MarkupLine($"[green]Found {files.Length} files for {fileType}[/]");

        // now we want to process each file
        foreach (var file in files)
        {
            Summary summary = new Summary(0, 0, 0, "N/A");
            //Console.WriteLine($"Processing file {file}");
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
                    // if the file had a header - remove that line from the results
                    if (fileConfig.HeaderLine)
                    {
                        results.RemoveAt(0);
                    }

                    // write to csv (we need to grab the records from the results)
                    var records = results.SelectMany(r => r.RecordResults ?? Enumerable.Empty<RecordResult>()).ToList();
                    FileWriter.TryWriteOrAppendToFile(records, $"{fileType}_results.csv");
                    return;
                }

                if (fileTypeToWrite == "json")
                {
                    // write to json
                    WriteToJson(file, results);
                    return;
                }
            }

            // Print summary of results
            Console.WriteLine("Summary of results: {0}", summary.ToString());
        } // end of file loop

        _logger.LogInformation("App::Completed");
    }

    /// <summary>
    /// Save the results to a JSON file
    /// This will create a new file in the outputs directory
    /// and write the results to it
    /// The file name will be the same as the input file
    /// but with a .json extension
    /// and the path will be ../files/outputs/
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="results"></param>
    void WriteToJson(string filePath, List<LineResult> results)
    {
        string outPutName = filePath.Replace(".csv", ".json").Split("/").Last();
        string outPutPath = Path.Combine("../files/outputs/", outPutName);

        // create JSON configuration for newtonsoft
        using StringWriter sw = new StringWriter();
        using JsonTextWriter writer = new JsonTextWriter(sw)
        {
            Formatting = Formatting.Indented,
            Indentation = 4,
            IndentChar = ' '
        };
        JsonSerializer serializer = new JsonSerializer();
        serializer.Serialize(writer, results);
        string json = sw.ToString();

        // write the json to a file
        using (StreamWriter file = File.CreateText(outPutPath))
        {
            file.WriteLine(json);
            file.Flush();
            file.Close();
            AnsiConsole.MarkupLine($"[green]Wrote results to {outPutPath}[/]");
        }
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