
using Spectre.Console;

public class App{

    public App()
    {
        
    }

    public void Run()
    {
        Console.WriteLine("App::Run");

        string fileType = CreateAndWaitForResponse("Select file type", new string[] { "transaction", "account", "customer" });

        Console.WriteLine($"You selected {fileType}");

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