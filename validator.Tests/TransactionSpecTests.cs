using Newtonsoft.Json;
using validator.Models;
using Xunit;

namespace validator.Tests;

/// <summary>
/// Guards the shipped Transaction_Spec.json itself (not a copy), so typos in the data are caught.
/// </summary>
public class TransactionSpecTests
{
    private static FileConfig LoadTransactionSpec()
    {
        // The specs are not copied to the test output, so walk up to the repo's validator/Specifications folder
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "validator", "Specifications", "Transaction_Spec.json");
            if (File.Exists(candidate))
            {
                return JsonConvert.DeserializeObject<FileConfig>(File.ReadAllText(candidate))!;
            }
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Transaction_Spec.json not found above " + AppContext.BaseDirectory);
    }

    [Fact]
    public void CardType_AllowsVisa_AndNotTheVasaTypo()
    {
        var cardType = Assert.Single(LoadTransactionSpec().ValidationConfigs, c => c.Name == "CardType");

        Assert.NotNull(cardType.AllowedValues);
        Assert.Contains("Visa", cardType.AllowedValues);
        Assert.DoesNotContain("Vasa", cardType.AllowedValues);
        Assert.NotNull(cardType.ErrorMessage);
        Assert.Contains("Visa", cardType.ErrorMessage);
        Assert.DoesNotContain("Vasa", cardType.ErrorMessage);
    }
}
