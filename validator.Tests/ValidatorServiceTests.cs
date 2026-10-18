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
            File.WriteAllLines(tempFile, [","]); // line 1 has an empty required field
            var config = new FileConfig("test", ",", false,
            [
                new ValidationConfig("CustomerId", "string", 0, false, null, null, [], [], "Customer ID required.") { Name = "CustomerId" },
                new ValidationConfig("Notes", "string", 1, true, null, null, [], [], null) { Name = "Notes" }
            ]);
            var summary = new Summary(0, 0, 0, "N/A");

            // Act
            var results = _validatorService.ProcessFileWithConfig(tempFile, config, summary);

            // Assert
            Assert.Single(results);
            var lineResult = results[0];
            Assert.NotNull(lineResult.RecordResults);
            var recordResult = Assert.Single(lineResult.RecordResults, r => !r.Valid);
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
    public void ProcessFileWithConfig_SkipsBlankAndTrailingEmptyLines()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "ABC\n\n  \nDEF\n\n\n");
            var config = new FileConfig("test", ",", false,
            [
                new ValidationConfig("CustomerId", "string", 0, false, null, null, [], [], null) { Name = "CustomerId" }
            ]);

            var results = _validatorService.ProcessFileWithConfig(tempFile, config, new Summary(0, 0, 0, "N/A"));

            Assert.Equal([1, 4], results.Select(r => r.LineCount));
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

    [Fact]
    public void ProcessFileWithConfig_PopulatesSummary_WithTotalsAndFileName()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tempFile, ["ABC", "", "DEF"]);
            _mockTypeValidator.Setup(v => v.ValidateType("ABC", "string", It.IsAny<string[]>())).Returns(true);
            _mockTypeValidator.Setup(v => v.ValidateType("DEF", "string", It.IsAny<string[]>())).Returns(false);

            var config = new FileConfig("test", ",", false,
            [
                new ValidationConfig("Id", "string", 0, false, null, null, [], [], "Invalid id.") { Name = "Id" }
            ]);
            var summary = new Summary(0, 0, 0, "N/A");

            _validatorService.ProcessFileWithConfig(tempFile, config, summary);

            Assert.Equal(Path.GetFileName(tempFile), summary.FileName);
            Assert.Equal(2, summary.TotalRecords);
            Assert.Equal(1, summary.ValidRecords);
            Assert.Equal(1, summary.InvalidRecords);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ProcessFileWithConfig_LineFails_WhenRowHasExtraUnconfiguredColumns()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tempFile, ["A,B,EXTRA"]);
            var config = new FileConfig("test", ",", false,
            [
                new ValidationConfig("Col1", "string", 0, false, null, null, [], [], "") { Name = "Col1" },
                new ValidationConfig("Col2", "string", 1, false, null, null, [], [], "") { Name = "Col2" }
            ]);
            _mockTypeValidator.Setup(v => v.ValidateType(It.IsAny<string>(), "string", It.IsAny<string[]>())).Returns(true);

            var results = _validatorService.ProcessFileWithConfig(tempFile, config, new Summary(0, 0, 0, "N/A"));

            var lineResult = Assert.Single(results);
            Assert.False(lineResult.Valid);
            Assert.Contains("Field index 2 not found in validation configs", lineResult.Message);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ProcessFileWithConfig_DescriptiveError_WhenMaxLengthExceeded()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tempFile, ["TOOLONG"]);
            var config = new FileConfig("test", ",", false,
            [
                new ValidationConfig("Code", "string", 0, false, null, 3, [], [], "Max 3 chars.") { Name = "Code" }
            ]);

            var results = _validatorService.ProcessFileWithConfig(tempFile, config, new Summary(0, 0, 0, "N/A"));

            var lineResult = Assert.Single(results);
            Assert.False(lineResult.Valid);
            var record = Assert.Single(lineResult.RecordResults!);
            Assert.False(record.Valid);
            Assert.Contains("exceeds maximum length (3)", record.ErrorMessage);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ProcessFileWithConfig_ValidMultiFieldRow_PassesValidation()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tempFile, ["ID1,Active,2025-01-01"]);
            var config = new FileConfig("test", ",", false,
            [
                new ValidationConfig("Id", "string", 0, false, 1, 10, [], [], "") { Name = "Id" },
                new ValidationConfig("Status", "string", 1, false, null, null, ["Active", "Inactive"], [], "") { Name = "Status" },
                new ValidationConfig("Date", "date", 2, false, null, null, [], ["yyyy-MM-dd"], "") { Name = "Date" }
            ]);
            _mockTypeValidator.Setup(v => v.ValidateType("ID1", "string", It.IsAny<string[]>())).Returns(true);
            _mockTypeValidator.Setup(v => v.ValidateType("Active", "string", It.IsAny<string[]>())).Returns(true);
            _mockTypeValidator.Setup(v => v.ValidateType("2025-01-01", "date", It.IsAny<string[]>())).Returns(true);

            var results = _validatorService.ProcessFileWithConfig(tempFile, config, new Summary(0, 0, 0, "N/A"));

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
    public void ValidatorService_SupportsCustomRuleInjection()
    {
        var customRuleMock = new Mock<validator.Services.Rules.IFieldRule>();
        customRuleMock.Setup(r => r.Validate(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<ValidationConfig>()))
            .Returns(new validator.Services.Rules.FieldRuleResult(
                new RecordResult(1, 0, "CustomCol", "VAL", false, "Custom rule failure"), false));

        var customService = new ValidatorService(_mockTypeValidator.Object, _mockHeaderValidator.Object, [customRuleMock.Object]);

        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tempFile, ["VAL"]);
            var config = new FileConfig("test", ",", false,
            [
                new ValidationConfig("CustomCol", "string", 0, false, null, null, [], [], "") { Name = "CustomCol" }
            ]);

            var results = customService.ProcessFileWithConfig(tempFile, config, new Summary(0, 0, 0, "N/A"));

            var lineResult = Assert.Single(results);
            Assert.False(lineResult.Valid);
            var record = Assert.Single(lineResult.RecordResults!);
            Assert.Equal("Custom rule failure", record.ErrorMessage);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
