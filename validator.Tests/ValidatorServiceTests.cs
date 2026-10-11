using Moq;
using validator.Models;
using validator.Services;
using validator.Services.Interfaces;
using Xunit;

namespace validator.Tests;

public class ValidatorServiceTests
{
    private readonly Mock<ITypeValidator> _mockTypeValidator;
    private readonly Mock<IHeaderValidator> _mockHeaderValidator;
    private readonly ValidatorService _validatorService;

    public ValidatorServiceTests()
    {
        _mockTypeValidator = new Mock<ITypeValidator>();
        _mockHeaderValidator = new Mock<IHeaderValidator>();
        _validatorService = new ValidatorService(_mockTypeValidator.Object, _mockHeaderValidator.Object);
    }

    [Fact]
    public void ProcessFileWithConfig_DescriptiveError_WhenFieldIsRequiredAndEmpty()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tempFile, [""]); // line 1 empty data row
            var config = new FileConfig("test", ",", false,
            [
                new ValidationConfig("CustomerId", "string", 0, false, null, null, [], [], "Customer ID required.") { Name = "CustomerId" }
            ]);
            var summary = new Summary(0, 0, 0, "N/A");

            // Act
            var results = _validatorService.ProcessFileWithConfig(tempFile, config, summary);

            // Assert
            Assert.Single(results);
            var lineResult = results[0];
            Assert.NotNull(lineResult.RecordResults);
            var recordResult = Assert.Single(lineResult.RecordResults);
            Assert.False(recordResult.Valid);
            Assert.Equal(0, recordResult.FieldIndex);
            Assert.Equal("CustomerId", recordResult.FieldName);
            Assert.Contains("Field 'CustomerId' at index 0: Field is required and cannot be empty", recordResult.ErrorMessage);
            Assert.Contains("Customer ID required.", recordResult.ErrorMessage);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ProcessFileWithConfig_DescriptiveError_WhenLengthOutOfRange()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tempFile, ["AB"]); // length 2
            var config = new FileConfig("test", ",", false,
            [
                new ValidationConfig("Currency", "string", 0, false, 3, 3, [], [], "Currency must be 3 characters.") { Name = "Currency" }
            ]);
            var summary = new Summary(0, 0, 0, "N/A");

            // Act
            var results = _validatorService.ProcessFileWithConfig(tempFile, config, summary);

            // Assert
            Assert.Single(results);
            var lineResult = results[0];
            Assert.NotNull(lineResult.RecordResults);
            var recordResult = Assert.Single(lineResult.RecordResults);
            Assert.False(recordResult.Valid);
            Assert.Equal(0, recordResult.FieldIndex);
            Assert.Equal("Currency", recordResult.FieldName);
            Assert.Equal("AB", recordResult.Key);
            Assert.Contains("Field 'Currency' at index 0: Value 'AB' length (2) is less than minimum length (3)", recordResult.ErrorMessage);
            Assert.Contains("Currency must be 3 characters.", recordResult.ErrorMessage);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ProcessFileWithConfig_DescriptiveError_WhenValueNotInAllowedValues()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tempFile, ["UNKNOWN"]);
            var config = new FileConfig("test", ",", false,
            [
                new ValidationConfig("Type", "string", 0, false, null, null, ["CREDIT", "DEBIT"], [], "Invalid type.") { Name = "Type" }
            ]);
            var summary = new Summary(0, 0, 0, "N/A");

            // Act
            var results = _validatorService.ProcessFileWithConfig(tempFile, config, summary);

            // Assert
            Assert.Single(results);
            var lineResult = results[0];
            Assert.NotNull(lineResult.RecordResults);
            var recordResult = Assert.Single(lineResult.RecordResults);
            Assert.False(recordResult.Valid);
            Assert.Equal(0, recordResult.FieldIndex);
            Assert.Equal("Type", recordResult.FieldName);
            Assert.Equal("UNKNOWN", recordResult.Key);
            Assert.Contains("Value 'UNKNOWN' is not in allowed values: [CREDIT, DEBIT]", recordResult.ErrorMessage);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ProcessFileWithConfig_LineIsValid_WhenOnlyNullableFieldIsEmpty()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tempFile, ["ABC,"]);
            var config = new FileConfig("test", ",", false,
            [
                new ValidationConfig("Id", "string", 0, false, null, null, [], [], "Id required.") { Name = "Id" },
                new ValidationConfig("Note", "string", 1, true, null, null, [], [], "Note optional.") { Name = "Note" }
            ]);
            _mockTypeValidator.Setup(v => v.ValidateType("ABC", "string", It.IsAny<string[]>())).Returns(true);
            var summary = new Summary(0, 0, 0, "N/A");

            // Act
            var results = _validatorService.ProcessFileWithConfig(tempFile, config, summary);

            // Assert
            var lineResult = Assert.Single(results);
            Assert.True(lineResult.Valid);
            Assert.Equal("Line is valid", lineResult.Message);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ProcessFileWithConfig_LineIsInvalid_WhenNullableFieldEmptyButAnotherFieldFails()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tempFile, [",", "ABC,"]);
            var config = new FileConfig("test", ",", false,
            [
                new ValidationConfig("Id", "string", 0, false, null, null, [], [], "Id required.") { Name = "Id" },
                new ValidationConfig("Note", "string", 1, true, null, null, [], [], "Note optional.") { Name = "Note" }
            ]);
            var summary = new Summary(0, 0, 0, "N/A");

            // Act
            var results = _validatorService.ProcessFileWithConfig(tempFile, config, summary);

            // Assert - first line has an empty required field alongside the empty nullable one
            Assert.False(results[0].Valid);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(false)] // missing column is required
    [InlineData(true)]  // missing column is nullable - the delimiter must still be present
    public void ProcessFileWithConfig_LineIsInvalid_WhenRowHasTooFewColumns(bool missingColumnNullable)
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tempFile, ["ABC"]);
            var config = new FileConfig("test", ",", false,
            [
                new ValidationConfig("Id", "string", 0, false, null, null, [], [], "Id required.") { Name = "Id" },
                new ValidationConfig("Note", "string", 1, missingColumnNullable, null, null, [], [], "Note error.") { Name = "Note" }
            ]);
            _mockTypeValidator.Setup(v => v.ValidateType("ABC", "string", It.IsAny<string[]>())).Returns(true);
            var summary = new Summary(0, 0, 0, "N/A");

            // Act
            var results = _validatorService.ProcessFileWithConfig(tempFile, config, summary);

            // Assert
            var lineResult = Assert.Single(results);
            Assert.False(lineResult.Valid);
            Assert.NotNull(lineResult.RecordResults);
            var missing = Assert.Single(lineResult.RecordResults, r => !r.Valid);
            Assert.Equal(1, missing.FieldIndex);
            Assert.Equal("Note", missing.FieldName);
            Assert.Contains("missing", missing.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ProcessFileWithConfig_DescriptiveError_WhenTypeValidationFails()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tempFile, ["ABC"]);
            _mockTypeValidator.Setup(v => v.ValidateType("ABC", "decimal", It.IsAny<string[]>())).Returns(false);

            var config = new FileConfig("test", ",", false,
            [
                new ValidationConfig("Amount", "decimal", 0, false, null, null, [], [], "Amount must be a decimal.") { Name = "Amount" }
            ]);
            var summary = new Summary(0, 0, 0, "N/A");

            // Act
            var results = _validatorService.ProcessFileWithConfig(tempFile, config, summary);

            // Assert
            Assert.Single(results);
            var lineResult = results[0];
            Assert.NotNull(lineResult.RecordResults);
            var recordResult = Assert.Single(lineResult.RecordResults);
            Assert.False(recordResult.Valid);
            Assert.Equal(0, recordResult.FieldIndex);
            Assert.Equal("Amount", recordResult.FieldName);
            Assert.Equal("ABC", recordResult.Key);
            Assert.NotNull(recordResult.ErrorMessage);
            Assert.Contains("Field 'Amount' at index 0: Value 'ABC' is not a valid decimal. Amount must be a decimal.", recordResult.ErrorMessage);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
