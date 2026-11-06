using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using validator.Headless;
using validator.Services;
using Xunit;

namespace validator.Tests;

public class HeadlessOptionsTests
{
    [Fact]
    public void Parse_NoArgs_ReturnsInteractiveMode()
    {
        var options = HeadlessOptions.Parse([]);
        Assert.False(options.IsHeadless);
        Assert.Equal(HeadlessCommand.None, options.Command);
        Assert.Null(options.Error);
    }

    [Fact]
    public void Parse_UnrelatedHostArgs_ReturnsInteractiveMode()
    {
        var options = HeadlessOptions.Parse(["--environment", "Development"]);
        Assert.False(options.IsHeadless);
    }

    [Fact]
    public void Parse_ListSpecs_ReturnsListCommand()
    {
        var options = HeadlessOptions.Parse(["--list-specs", "--spec-dir", "/tmp/specs"]);
        Assert.Equal(HeadlessCommand.ListSpecs, options.Command);
        Assert.Equal("/tmp/specs", options.SpecDirectory);
        Assert.Null(options.Error);
    }

    [Fact]
    public void Parse_SpecAndFile_ReturnsValidateCommand()
    {
        var options = HeadlessOptions.Parse(["--spec", "account", "--file", "a.csv", "--output", "out.json"]);
        Assert.Equal(HeadlessCommand.Validate, options.Command);
        Assert.Equal("account", options.Spec);
        Assert.Equal("a.csv", options.FilePath);
        Assert.Equal("out.json", options.OutputPath);
        Assert.Null(options.Error);
    }

    [Theory]
    [InlineData(new[] { "--spec", "account" }, "--file")]
    [InlineData(new[] { "--file", "a.csv" }, "--spec")]
    [InlineData(new[] { "--spec", "--file", "a.csv" }, "--spec")]
    [InlineData(new[] { "--spec", "account", "--file" }, "--file")]
    public void Parse_MissingValues_SetsError(string[] args, string expectedFlag)
    {
        var options = HeadlessOptions.Parse(args);
        Assert.Equal(HeadlessCommand.Validate, options.Command);
        Assert.NotNull(options.Error);
        Assert.Contains(expectedFlag, options.Error);
    }

    [Fact]
    public void Parse_ModifierWithoutCommand_IsUsageError()
    {
        var options = HeadlessOptions.Parse(["--output", "x.json"]);
        Assert.Equal(HeadlessCommand.Help, options.Command);
        Assert.NotNull(options.Error);
    }
}

public class HeadlessRunnerTests : IDisposable
{
    private readonly string _tempDir;

    public HeadlessRunnerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "headless-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private static (HeadlessRunner Runner, StringWriter Output, StringWriter Diagnostics) CreateRunner()
    {
        var typeValidator = new TypeValidator(NullLogger<TypeValidator>.Instance);
        var service = new ValidatorService(typeValidator, new HeaderValidator());
        var selector = new SpecificationSelector();
        var output = new StringWriter();
        var diagnostics = new StringWriter();
        return (new HeadlessRunner(service, selector, output, diagnostics), output, diagnostics);
    }

    private string WriteAccountFile(params string[] dataRows)
    {
        var path = Path.Combine(_tempDir, "account.csv");
        var lines = new List<string> { "AccountId|CustomerId|AccountType|AccountStatus|OpenDate|Currency|Balance" };
        lines.AddRange(dataRows);
        File.WriteAllLines(path, lines);
        return path;
    }

    [Fact]
    public void ListSpecs_EmitsCamelCaseSpecsWithColumns()
    {
        var (runner, output, _) = CreateRunner();

        var exit = runner.Run(HeadlessOptions.Parse(["--list-specs"]));

        Assert.Equal(HeadlessRunner.ExitValid, exit);
        var json = JObject.Parse(output.ToString());
        Assert.Equal(1, json["schemaVersion"]!.Value<int>());
        var specs = (JArray)json["specs"]!;
        var account = specs.First(s => s["fileType"]!.Value<string>() == "account");
        Assert.Equal("|", account["delimiter"]!.Value<string>());
        Assert.True(account["headerLine"]!.Value<bool>());
        Assert.Equal("AccountId", account["columns"]![0]!["name"]!.Value<string>());
    }

    [Fact]
    public void Validate_ValidFile_ReturnsExitZeroAndFullStats()
    {
        var file = WriteAccountFile(
            "68b8a813-0813-4883-f927-b0f4fed9769b|4fa5727b-2403-b7ee-758d-18c0c636ad0f|Current|Active|2024-01-15|GBP|1250.50");
        var (runner, output, _) = CreateRunner();

        var exit = runner.Run(HeadlessOptions.Parse(["--spec", "account", "--file", file]));

        Assert.Equal(HeadlessRunner.ExitValid, exit);
        var json = JObject.Parse(output.ToString());
        Assert.True(json["success"]!.Value<bool>());
        Assert.Equal("account", json["spec"]!.Value<string>());
        Assert.Equal(2, json["stats"]!["totalLines"]!.Value<int>());
        Assert.Equal(1, json["stats"]!["dataLines"]!.Value<int>());
        Assert.Equal(0, json["stats"]!["invalidLines"]!.Value<int>());
        Assert.Equal(100d, json["stats"]!["passRate"]!.Value<double>());
        Assert.True(json["stats"]!["headerValid"]!.Value<bool>());
        Assert.Equal(2, ((JArray)json["lines"]!).Count);
        Assert.Equal("account.csv", json["summary"]!["fileName"]!.Value<string>());
    }

    [Fact]
    public void Validate_InvalidRows_ReturnsExitOneWithFieldBreakdown()
    {
        var file = WriteAccountFile(
            "68b8a813-0813-4883-f927-b0f4fed9769b|4fa5727b-2403-b7ee-758d-18c0c636ad0f|Current|Active|2024-01-15|GBP|1250.50",
            "016c87fb-b2a1-ee94-74f2-adcc00448470|62379857-f545-808c-acd5-62d7130423f2|Current|Active|2024-02-20|GBP|not-a-number");
        var (runner, output, _) = CreateRunner();

        var exit = runner.Run(HeadlessOptions.Parse(["--spec", "account", "--file", file]));

        Assert.Equal(HeadlessRunner.ExitInvalid, exit);
        var json = JObject.Parse(output.ToString());
        Assert.Equal(1, json["stats"]!["invalidLines"]!.Value<int>());
        Assert.Equal(50d, json["stats"]!["passRate"]!.Value<double>());
        Assert.Equal(1, json["stats"]!["errorsByField"]!["Balance"]!.Value<int>());

        var badLine = ((JArray)json["lines"]!).First(l => !l["valid"]!.Value<bool>());
        Assert.Equal(3, badLine["lineCount"]!.Value<int>());
        var record = ((JArray)badLine["recordResults"]!).First(r => !r["valid"]!.Value<bool>());
        Assert.Equal(6, record["fieldIndex"]!.Value<int>());
        Assert.Equal("Balance", record["fieldName"]!.Value<string>());
        Assert.Equal("not-a-number", record["key"]!.Value<string>());
    }

    [Fact]
    public void Validate_UnknownSpec_ReturnsErrorPayload()
    {
        var file = WriteAccountFile();
        var (runner, output, diagnostics) = CreateRunner();

        var exit = runner.Run(HeadlessOptions.Parse(["--spec", "nope", "--file", file]));

        Assert.Equal(HeadlessRunner.ExitError, exit);
        var json = JObject.Parse(output.ToString());
        Assert.False(json["success"]!.Value<bool>());
        Assert.Contains("Unknown specification 'nope'", json["error"]!.Value<string>());
        Assert.Contains("account", json["error"]!.Value<string>());
        Assert.Contains("Unknown specification", diagnostics.ToString());
    }

    [Fact]
    public void Validate_MissingFile_ReturnsErrorPayload()
    {
        var (runner, output, _) = CreateRunner();

        var exit = runner.Run(HeadlessOptions.Parse(["--spec", "account", "--file", Path.Combine(_tempDir, "missing.csv")]));

        Assert.Equal(HeadlessRunner.ExitError, exit);
        var json = JObject.Parse(output.ToString());
        Assert.False(json["success"]!.Value<bool>());
        Assert.Contains("File not found", json["error"]!.Value<string>());
    }

    [Fact]
    public void Validate_WithOutputPath_WritesJsonFileAndLeavesStdoutEmpty()
    {
        var file = WriteAccountFile(
            "68b8a813-0813-4883-f927-b0f4fed9769b|4fa5727b-2403-b7ee-758d-18c0c636ad0f|Current|Active|2024-01-15|GBP|1250.50");
        var outPath = Path.Combine(_tempDir, "nested", "report.json");
        var (runner, output, _) = CreateRunner();

        var exit = runner.Run(HeadlessOptions.Parse(["--spec", "account", "--file", file, "--output", outPath]));

        Assert.Equal(HeadlessRunner.ExitValid, exit);
        Assert.Equal(string.Empty, output.ToString());
        Assert.True(File.Exists(outPath));
        Assert.True(JObject.Parse(File.ReadAllText(outPath))["success"]!.Value<bool>());
    }

    [Fact]
    public void Run_UsageError_ReturnsExitTwoAndPrintsUsage()
    {
        var (runner, output, diagnostics) = CreateRunner();

        var exit = runner.Run(HeadlessOptions.Parse(["--output", "x.json"]));

        Assert.Equal(HeadlessRunner.ExitError, exit);
        Assert.Equal(string.Empty, output.ToString());
        Assert.Contains("Headless usage", diagnostics.ToString());
    }
}
