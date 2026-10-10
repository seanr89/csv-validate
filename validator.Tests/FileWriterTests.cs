using validator.Models;
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
        FileWriter.TryWriteOrAppendToFile([new RecordResult(1, "A", false, "first")], "results.csv");
        FileWriter.TryWriteOrAppendToFile([new RecordResult(2, "B", false, "second")], "results.csv");

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
        FileWriter.TryWriteOrAppendToFile([new RecordResult(1, "A", false, "first")], "results.csv");

        // Assert
        var lines = File.ReadAllLines(Path.Combine(outputs, "results.csv"));
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("LineCount", lines[0]);
        Assert.Contains("first", lines[1]);
    }
}
