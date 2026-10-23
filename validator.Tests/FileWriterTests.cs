using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using validator.Models;
using validator.Services;
using validator.Services.Interfaces;
using Xunit;

namespace validator.Tests;

// FileWriter writes to ../files/outputs relative to the working directory,
// so these tests change it and must not run alongside other tests.
[CollectionDefinition("WorkingDirectory", DisableParallelization = true)]
public class WorkingDirectoryCollection;

[Collection("WorkingDirectory")]
public class FileWriterTests : IDisposable
{
    private readonly string _originalDirectory = Directory.GetCurrentDirectory();
    private readonly string _root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly IFileWriter _fileWriter = new FileWriter(NullLogger<FileWriter>.Instance);

    public FileWriterTests()
    {
        // Mirror the app layout: run from <root>/validator so ../files/outputs resolves under <root>
        var workDir = Path.Combine(_root, "validator");
        Directory.CreateDirectory(workDir);
        Directory.SetCurrentDirectory(workDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    [Fact]
    public void TryWriteOrAppendToFile_WritesHeaderOnce_WhenAppendingToExistingFile()
    {
        // Act
        _fileWriter.TryWriteOrAppendToFile([new RecordResult(1, "A", false, "first")], "results.csv");
        _fileWriter.TryWriteOrAppendToFile([new RecordResult(2, "B", false, "second")], "results.csv");

        // Assert
        var lines = File.ReadAllLines(Path.Combine(_root, "files", "outputs", "results.csv"));
        Assert.Equal(3, lines.Length);
        Assert.Single(lines, l => l.StartsWith("LineCount"));
        Assert.Contains("first", lines[1]);
        Assert.Contains("second", lines[2]);
    }

    [Fact]
    public void TryWriteOrAppendToFile_WritesHeader_WhenExistingFileIsEmpty()
    {
        // Arrange
        var outputs = Path.Combine(_root, "files", "outputs");
        Directory.CreateDirectory(outputs);
        File.WriteAllText(Path.Combine(outputs, "results.csv"), string.Empty);

        // Act
        _fileWriter.TryWriteOrAppendToFile([new RecordResult(1, "A", false, "first")], "results.csv");

        // Assert
        var lines = File.ReadAllLines(Path.Combine(outputs, "results.csv"));
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("LineCount", lines[0]);
        Assert.Contains("first", lines[1]);
    }

    [Fact]
    public void WriteToJson_CreatesOutputDirectory_WhenItDoesNotExist()
    {
        // Arrange
        var outputs = Path.Combine(_root, "files", "outputs");
        Assert.False(Directory.Exists(outputs));

        // Act
        _fileWriter.WriteToJson("../files/transactions.csv", [new LineResult(1, true, "Line is valid")]);

        // Assert
        var jsonPath = Path.Combine(outputs, "transactions.json");
        Assert.True(File.Exists(jsonPath));
        Assert.Contains("Line is valid", File.ReadAllText(jsonPath));
    }

    [Fact]
    public void WriteToJson_ExtractsFileName_FromWindowsStylePath()
    {
        // Act
        _fileWriter.WriteToJson(@"..\files\transactions.csv", [new LineResult(1, true, "Line is valid")]);

        // Assert
        var jsonPath = Path.Combine(_root, "files", "outputs", "transactions.json");
        Assert.True(File.Exists(jsonPath));
    }

    [Fact]
    public void TryWriteOrAppendToFile_WritesToCustomOutputDirectory_WhenProvided()
    {
        var customDir = Path.Combine(_root, "custom_out");

        _fileWriter.TryWriteOrAppendToFile([new RecordResult(1, "A", false, "custom")], "custom.csv", outputDirectory: customDir);

        var filePath = Path.Combine(customDir, "custom.csv");
        Assert.True(File.Exists(filePath));
        var lines = File.ReadAllLines(filePath);
        Assert.Equal(2, lines.Length);
        Assert.Contains("custom", lines[1]);
    }

    [Fact]
    public void WriteToJson_WritesToCustomOutputDirectory_WhenProvided()
    {
        var customDir = Path.Combine(_root, "custom_json_out");

        _fileWriter.WriteToJson("test.csv", [new LineResult(1, true, "Valid custom")], outputDirectory: customDir);

        var filePath = Path.Combine(customDir, "test.json");
        Assert.True(File.Exists(filePath));
        Assert.Contains("Valid custom", File.ReadAllText(filePath));
    }

    [Fact]
    public void FileWriter_CanBeResolvedFromDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddTransient<IFileWriter, FileWriter>();
        var provider = services.BuildServiceProvider();

        var instance = provider.GetService<IFileWriter>();
        Assert.NotNull(instance);
        Assert.IsType<FileWriter>(instance);
    }

    private record DateRecord(string Name, DateTime Timestamp);

    [Fact]
    public void WriteToFile_SupportsCustomDelimiterAndDateFormat()
    {
        var customDir = Path.Combine(_root, "custom_delim_out");
        var data = new[] { new DateRecord("Event1", new DateTime(2025, 12, 31)) };

        _fileWriter.WriteToFile(data, "custom.csv", writeHeader: true, delimiter: ";", dateFormat: "yyyy-MM-dd", outputDirectory: customDir);

        var filePath = Path.Combine(customDir, "custom.csv");
        Assert.True(File.Exists(filePath));
        var lines = File.ReadAllLines(filePath);
        Assert.Equal(2, lines.Length);
        Assert.Contains(";", lines[0]);
        Assert.Contains("2025-12-31", lines[1]);
        Assert.Contains("Event1;2025-12-31", lines[1]);
    }

    [Fact]
    public void WriteToFile_PropagatesException_WhenPathIsInvalid()
    {
        // An invalid path containing a null char or using a file path as directory
        var blockerFile = Path.Combine(_root, "blocker.txt");
        File.WriteAllText(blockerFile, "I am a file, not a directory");

        var data = new[] { new RecordResult(1, "Key", true, null) };

        // Attempting to treat blockerFile as a directory will fail with IOException
        Assert.ThrowsAny<Exception>(() =>
            _fileWriter.WriteToFile(data, "output.csv", outputDirectory: Path.Combine(blockerFile, "sub")));
    }

    [Fact]
    public void WriteToJson_PropagatesException_WhenPathIsInvalid()
    {
        var blockerFile = Path.Combine(_root, "blocker_json.txt");
        File.WriteAllText(blockerFile, "I am a file, not a directory");

        var results = new List<LineResult> { new(1, true, "valid") };

        Assert.ThrowsAny<Exception>(() =>
            _fileWriter.WriteToJson("test.csv", results, outputDirectory: Path.Combine(blockerFile, "sub")));
    }
}
