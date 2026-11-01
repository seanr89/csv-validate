using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using validator.Models;
using validator.Services;
using Xunit;

namespace validator.Tests;

public class AccountSpecTests
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

    private static FileConfig LoadAccountSpec()
    {
        var root = FindRepoRoot();
        var candidate = Path.Combine(root, "validator", "Specifications", "Account_Spec.json");
        if (File.Exists(candidate))
        {
            return JsonConvert.DeserializeObject<FileConfig>(File.ReadAllText(candidate))!;
        }
        throw new FileNotFoundException("Account_Spec.json not found at " + candidate);
    }

    [Fact]
    public void AccountSpec_HasExpectedColumnDefinitions()
    {
        var spec = LoadAccountSpec();

        Assert.Equal("account", spec.FileType);
        Assert.Equal("|", spec.Delimiter);
        Assert.True(spec.HeaderLine);
        Assert.Equal(7, spec.ValidationConfigs.Count);

        var names = spec.ValidationConfigs.Select(c => c.Name).ToList();
        Assert.Equal(["AccountId", "CustomerId", "AccountType", "AccountStatus", "OpenDate", "Currency", "Balance"], names);

        var accountType = spec.ValidationConfigs.First(c => c.Name == "AccountType");
        Assert.NotNull(accountType.AllowedValues);
        Assert.Contains("Current", accountType.AllowedValues);
        Assert.Contains("Savings", accountType.AllowedValues);
        Assert.Contains("Credit", accountType.AllowedValues);

        var accountStatus = spec.ValidationConfigs.First(c => c.Name == "AccountStatus");
        Assert.NotNull(accountStatus.AllowedValues);
        Assert.Contains("Active", accountStatus.AllowedValues);
        Assert.Contains("Closed", accountStatus.AllowedValues);
        Assert.Contains("Suspended", accountStatus.AllowedValues);
    }

    [Fact]
    public void EndToEnd_ValidateAccountCsv_WithAccountSpec()
    {
        var root = FindRepoRoot();
        var csvPath = Path.Combine(root, "files", "account.csv");
        Assert.True(File.Exists(csvPath), $"Data file not found at {csvPath}");

        var spec = LoadAccountSpec();
        var typeValidator = new TypeValidator(NullLogger<TypeValidator>.Instance);
        var headerValidator = new HeaderValidator();
        var service = new ValidatorService(typeValidator, headerValidator);

        var summary = new Summary(0, 0, 0, "N/A");
        var results = service.ProcessFileWithConfig(csvPath, spec, summary);

        // Header line (1) + 5 account data lines = 6 total results
        Assert.Equal(6, results.Count);
        Assert.Equal("account.csv", summary.FileName);
        Assert.Equal(6, summary.TotalRecords);
        Assert.Equal(6, summary.ValidRecords);
        Assert.Equal(0, summary.InvalidRecords);
    }

    [Fact]
    public void SpecificationSelector_IncludesAccountInAvailableFileTypes()
    {
        var selector = new SpecificationSelector();
        var availableTypes = selector.GetAvailableFileTypes();

        Assert.Contains("account", availableTypes);
        var config = selector.GetFileConfig("account");
        Assert.NotNull(config);
        Assert.NotEmpty(config.ValidationConfigs);
    }
}
