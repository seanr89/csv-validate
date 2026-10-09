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
                new ValidationConfig("CustomerId", "string", 0, false, null, null, false, [], [], "Customer ID required.") { Name = "CustomerId" }
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
                new ValidationConfig("Currency", "string", 0, false, 3, 3, false, [], [], "Currency must be 3 characters.") { Name = "Currency" }
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
                new ValidationConfig("Type", "string", 0, false, null, null, false, ["CREDIT", "DEBIT"], [], "Invalid type.") { Name = "Type" }
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
                new ValidationConfig("Amount", "decimal", 0, false, null, null, false, [], [], "Amount must be a decimal.") { Name = "Amount" }
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
