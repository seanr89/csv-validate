using System.Reflection;
using Newtonsoft.Json;
using validator.Models;
using Xunit;

namespace validator.Tests;

public class ValidationConfigTests
{
    [Fact]
    public void ValidationConfig_HasOnlyPascalCaseProperties_NoDuplicateLowercaseProperties()
    {
        var properties = typeof(ValidationConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var propertyNames = properties.Select(p => p.Name).ToList();

        // Ensure all public properties follow PascalCase naming
        Assert.All(propertyNames, name => Assert.True(char.IsUpper(name[0]), $"Property '{name}' is not PascalCase"));

        // Ensure positional record duplicate lowercase properties are gone
        string[] lowercasePropertyNames =
        [
            "name",
            "type",
            "index",
            "isNullable",
            "minLength",
            "maxLength",
            "allowedValues",
            "formats",
            "errorMessage"
        ];

        foreach (var lowerProp in lowercasePropertyNames)
        {
            Assert.DoesNotContain(lowerProp, propertyNames);
        }
    }

    [Fact]
    public void ValidationConfig_CanBeInitializedWithRequiredInit()
    {
        var config = new ValidationConfig
        {
            Name = "TransactionId",
            Type = "string",
            Index = 1,
            IsNullable = true,
            MinLength = 5,
            MaxLength = 20,
            AllowedValues = ["A", "B"],
            Formats = ["yyyy-MM-dd"],
            ErrorMessage = "Custom error"
        };

        Assert.Equal("TransactionId", config.Name);
        Assert.Equal("string", config.Type);
        Assert.Equal(1, config.Index);
        Assert.True(config.IsNullable);
        Assert.Equal(5, config.MinLength);
        Assert.Equal(20, config.MaxLength);
        Assert.Equal(["A", "B"], config.AllowedValues);
        Assert.Equal(["yyyy-MM-dd"], config.Formats);
        Assert.Equal("Custom error", config.ErrorMessage);
    }

    [Fact]
    public void ValidationConfig_CanBeInitializedWithConstructor()
    {
        var config = new ValidationConfig(
            name: "Amount",
            type: "decimal",
            index: 2,
            isNullable: false,
            minLength: 1,
            maxLength: 10,
            allowedValues: ["10.00", "20.00"],
            formats: [],
            errorMessage: "Invalid amount");

        Assert.Equal("Amount", config.Name);
        Assert.Equal("decimal", config.Type);
        Assert.Equal(2, config.Index);
        Assert.False(config.IsNullable);
        Assert.Equal(1, config.MinLength);
        Assert.Equal(10, config.MaxLength);
        Assert.Equal(["10.00", "20.00"], config.AllowedValues);
        Assert.Empty(config.Formats!);
        Assert.Equal("Invalid amount", config.ErrorMessage);
    }

    [Fact]
    public void ValidationConfig_DeserializesCorrectlyFromJson()
    {
        const string json = """
        {
            "name": "TransactionDate",
            "type": "DateTime",
            "index": 3,
            "isNullable": false,
            "minLength": 10,
            "maxLength": 10,
            "allowedValues": [],
            "formats": ["yyyy-MM-dd", "dd/MM/yyyy"],
            "errorMessage": "Invalid date"
        }
        """;

        var config = JsonConvert.DeserializeObject<ValidationConfig>(json);

        Assert.NotNull(config);
        Assert.Equal("TransactionDate", config.Name);
        Assert.Equal("DateTime", config.Type);
        Assert.Equal(3, config.Index);
        Assert.False(config.IsNullable);
        Assert.Equal(10, config.MinLength);
        Assert.Equal(10, config.MaxLength);
        Assert.NotNull(config.AllowedValues);
        Assert.Empty(config.AllowedValues);
        Assert.NotNull(config.Formats);
        Assert.Equal(["yyyy-MM-dd", "dd/MM/yyyy"], config.Formats);
        Assert.Equal("Invalid date", config.ErrorMessage);
    }
}
