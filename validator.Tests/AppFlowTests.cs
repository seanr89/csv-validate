using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using validator.Models;
using validator.Services;
using Xunit;

namespace validator.Tests;

public class AppFlowTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "csv-validate.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Repository root not found above " + AppContext.BaseDirectory);
    }

    [Fact]
    public void EndToEnd_ValidateTransactionsCsv_WithRealSpec()
    {
        var root = FindRepoRoot();
        var specPath = Path.Combine(root, "validator", "Specifications", "Transaction_Spec.json");
        var csvPath = Path.Combine(root, "files", "transactions.csv");

        Assert.True(File.Exists(specPath), $"Spec file not found at {specPath}");
        Assert.True(File.Exists(csvPath), $"Data file not found at {csvPath}");

        var spec = JsonConvert.DeserializeObject<FileConfig>(File.ReadAllText(specPath));
        Assert.NotNull(spec);

        var typeValidator = new TypeValidator(NullLogger<TypeValidator>.Instance);
        var headerValidator = new HeaderValidator();
        var service = new ValidatorService(typeValidator, headerValidator);

        var summary = new Summary(0, 0, 0, "N/A");
        var results = service.ProcessFileWithConfig(csvPath, spec, summary);

        // Header line (1) + 4000 transaction data lines = 4001 total results
        Assert.Equal(4001, results.Count);
        Assert.Equal("transactions.csv", summary.FileName);
        Assert.Equal(4001, summary.TotalRecords);

        // Header line should be valid
        Assert.True(results[0].Valid, "Header line should be valid");

        // Verify summary matches results counts
        Assert.Equal(results.Count(r => r.Valid), summary.ValidRecords);
        Assert.Equal(results.Count(r => !r.Valid), summary.InvalidRecords);
        // All rows in the shipped transactions.csv are valid transactions
        Assert.Equal(4001, summary.ValidRecords);
        Assert.Equal(0, summary.InvalidRecords);
    }

    [Fact]
    public void App_ResolveDirectoryPath_ResolvesCorrectly()
    {
        var root = FindRepoRoot();
        var resolvedFiles = App.ResolveDirectoryPath("files", "files", mustExist: true);
        Assert.True(Directory.Exists(resolvedFiles));
        Assert.True(File.Exists(Path.Combine(resolvedFiles, "transactions.csv")));
    }

    [Fact]
    public void SpecificationSelector_ResolveSpecificationsDirectory_ResolvesCorrectly()
    {
        var specDir = SpecificationSelector.ResolveSpecificationsDirectory();
        Assert.True(Directory.Exists(specDir));
        Assert.True(File.Exists(Path.Combine(specDir, "Transaction_Spec.json")));
    }
}
