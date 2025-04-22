
using Microsoft.Extensions.Logging;
using Spectre.Console;

public class App{

    private readonly ILogger<App> _logger;
    private readonly ValidatorService _validatorService;
    private readonly SpecificationSelector _specificationSelector;

    public App(ILogger<App> logger, ValidatorService validatorService, SpecificationSelector specificationSelector)
    {
        _logger = logger;
        _validatorService = validatorService;
        _specificationSelector = specificationSelector;
    }

    public void Run()
    {
        Console.WriteLine("App::Run");

        string fileType = CreateAndWaitForResponse("Select file type", new string[] { "transactions", "account", "customer" });
        //Console.WriteLine($"You selected {fileType}");

        var fileConfig = _specificationSelector.GetFileConfig(fileType);
        if (fileConfig == null)
        {
            Console.WriteLine($"Error: Unable to find file config for {fileType}");
            return;
        }

        // go grab the files for the type
        var files = Directory.GetFiles("../files/", $"{fileType}.csv");
        if(files.Length == 0)
        {
            Console.WriteLine($"Error: No files found for {fileType}");
            return;
        }

        // now we want to process each file
        foreach (var file in files)
        {
            //Console.WriteLine($"Processing file {file}");
            var results = _validatorService.ProcessFileWithConfig(file, fileConfig);
            if (results == null)
            {
                Console.WriteLine($"Error: Unable to process file {file}");
                continue;
            }
            else{
                var count = results.Count(r => r.Valid == false);
                if (count > 0)
                {
                    Console.WriteLine($"File {file} has {count} errors");
                }
                else
                {
                    Console.WriteLine($"File {file} has no errors");
                }
            }
        }

        Console.WriteLine("App::Completed");
    }

    /// <summary>
    /// Create selection prompt and wait for response
    /// </summary>
    /// <param name="message"></param>
    /// <param name="choices"></param>
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