using System.Collections.Generic;
using Xunit;
using validator.Models;

namespace validator.Tests;

public class ValidationUtilsTests
{
    private static FileConfig CreateMockFileConfig(string delimiter = ",")
    {
        return new FileConfig("test", delimiter, true, new List<ValidationConfig>());
    }

    [Theory]
    [InlineData(1, 3, "Header is missing 2 column(s)")]
    [InlineData(2, 5, "Header is missing 3 column(s)")]
    public void ScanForNullOrExtraColumns_MissingColumns_AddsCorrectError(int fieldsCount, int expectedCount, string expectedErrorMessage)
    {
        // Arrange
        var fileConfig = CreateMockFileConfig(",");
        var results = new List<RecordResult>();
        var fields = new string[fieldsCount];
        for (int i = 0; i < fieldsCount; i++)
        {
            fields[i] = $"Col{i + 1}";
        }
        int lineCount = 10;

        // Act
        ValidationUtils.ScanForNullOrExtraColumns(lineCount, fileConfig, results, fields, expectedCount);

        // Assert
        var result = Assert.Single(results);
        Assert.False(result.Valid);
        Assert.Equal(lineCount, result.LineCount);
        Assert.Equal(string.Join(",", fields), result.Key);
        Assert.Equal(expectedErrorMessage, result.ErrorMessage);
    }

    [Theory]
    [InlineData(3, 2, "Header has 1 extra column(s)")]
    [InlineData(5, 2, "Header has 3 extra column(s)")]
    public void ScanForNullOrExtraColumns_ExtraColumns_AddsCorrectError(int fieldsCount, int expectedCount, string expectedErrorMessage)
    {
        // Arrange
        var fileConfig = CreateMockFileConfig(",");
        var results = new List<RecordResult>();
        var fields = new string[fieldsCount];
        for (int i = 0; i < fieldsCount; i++)
        {
            fields[i] = $"Col{i + 1}";
        }
        int lineCount = 15;

        // Act
        ValidationUtils.ScanForNullOrExtraColumns(lineCount, fileConfig, results, fields, expectedCount);

        // Assert
        var result = Assert.Single(results);
        Assert.False(result.Valid);
        Assert.Equal(lineCount, result.LineCount);
        Assert.Equal(string.Join(",", fields), result.Key);
        Assert.Equal(expectedErrorMessage, result.ErrorMessage);
    }

    [Fact]
    public void ScanForNullOrExtraColumns_CorrectColumnsCount_DoesNotAddError()
    {
        // Arrange
        var fileConfig = CreateMockFileConfig(",");
        var results = new List<RecordResult>();
        var fields = new[] { "Col1", "Col2", "Col3" };
        int expectedCount = 3;
        int lineCount = 20;

        // Act
        ValidationUtils.ScanForNullOrExtraColumns(lineCount, fileConfig, results, fields, expectedCount);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(",")]
    [InlineData(";")]
    [InlineData("|")]
    [InlineData("\t")]
    public void ScanForNullOrExtraColumns_UsesDelimiterFromFileConfig(string delimiter)
    {
        // Arrange
        var fileConfig = CreateMockFileConfig(delimiter);
        var results = new List<RecordResult>();
        var fields = new[] { "Col1", "Col2" };
        int expectedCount = 3;
        int lineCount = 1;

        // Act
        ValidationUtils.ScanForNullOrExtraColumns(lineCount, fileConfig, results, fields, expectedCount);

        // Assert
        var result = Assert.Single(results);
        Assert.Equal($"Col1{delimiter}Col2", result.Key);
    }

    [Fact]
    public void ScanForNullOrExtraColumns_EmptyFieldsAndExpectedCountGreaterThanZero_AddsMissingColumnsError()
    {
        // Arrange
        var fileConfig = CreateMockFileConfig(",");
        var results = new List<RecordResult>();
        var fields = System.Array.Empty<string>();
        int expectedCount = 2;
        int lineCount = 5;

        // Act
        ValidationUtils.ScanForNullOrExtraColumns(lineCount, fileConfig, results, fields, expectedCount);

        // Assert
        var result = Assert.Single(results);
        Assert.False(result.Valid);
        Assert.Equal("Header is missing 2 column(s)", result.ErrorMessage);
        Assert.Equal("", result.Key);
    }

    [Fact]
    public void ScanForNullOrExtraColumns_ExpectedCountZeroAndFieldsNotEmpty_AddsExtraColumnsError()
    {
        // Arrange
        var fileConfig = CreateMockFileConfig(",");
        var results = new List<RecordResult>();
        var fields = new[] { "Col1" };
        int expectedCount = 0;
        int lineCount = 8;

        // Act
        ValidationUtils.ScanForNullOrExtraColumns(lineCount, fileConfig, results, fields, expectedCount);

        // Assert
        var result = Assert.Single(results);
        Assert.False(result.Valid);
        Assert.Equal("Header has 1 extra column(s)", result.ErrorMessage);
        Assert.Equal("Col1", result.Key);
    }
}
