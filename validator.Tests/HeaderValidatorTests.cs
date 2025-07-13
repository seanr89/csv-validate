using Moq;
using validator.Models;
using validator.Services;
using validator.Services.Interfaces;

namespace validator.Tests;

public class HeaderValidatorTests
{
    private readonly Mock<IHeaderValidator> _mockHeaderValidator;

    public HeaderValidatorTests()
    {
        _mockHeaderValidator = new Mock<IHeaderValidator>();
    }

    [Fact]
    public void Validate_HeaderWithMissingColumns_ReturnsError()
    {
        // Arrange
        var fileConfig = new FileConfig("test", ",", true, new List<ValidationConfig>
        {
            new("Id", "int", 0, false, null, null, false, [], [], "Invalid Id") { Name = "Id" },
            new("Name", "string", 1, false, null, null, false, [], [], "Invalid Name") { Name = "Name" },
            new("Age", "int", 2, false, null, null, false, [], [], "Invalid Age") { Name = "Age" }
        });
        var line = "Id,Name";
        var headerValidator = new HeaderValidator();

        // Act
        var results = headerValidator.Validate(1, line, fileConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage.Contains("Header is missing 1 column(s)"));
    }

    [Fact]
    public void Validate_HeaderWithExtraColumns_ReturnsError()
    {
        // Arrange
        var fileConfig = new FileConfig("test", ",", true, new List<ValidationConfig>
        {
            new("Id", "int", 0, false, null, null, false, [], [], "Invalid Id") { Name = "Id" },
            new("Name", "string", 1, false, null, null, false, [], [], "Invalid Name") { Name = "Name" }
        });
        var line = "Id,Name,Age";
        var headerValidator = new HeaderValidator();

        // Act
        var results = headerValidator.Validate(1, line, fileConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage.Contains("Header has 1 extra column(s)"));
    }

    [Fact]
    public void Validate_HeaderWithMismatchedField_ReturnsError()
    {
        // Arrange
        var fileConfig = new FileConfig("test", ",", true, new List<ValidationConfig>
        {
            new("Id", "int", 0, false, null, null, false, [], [], "Invalid Id") { Name = "Id" },
            new("Name", "string", 1, false, null, null, false, [], [], "Invalid Name") { Name = "Name" },
            new("Age", "int", 2, false, null, null, false, [], [], "Invalid Age") { Name = "Age" }
        });
        var line = "Id,FullName,Age";
        var headerValidator = new HeaderValidator();

        // Act
        var results = headerValidator.Validate(1, line, fileConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage.Contains("Header field 'FullName' not found in validation config"));
    }

    [Fact]
    public void Validate_ValidHeader_ReturnsNoErrors()
    {
        // Arrange
        var fileConfig = new FileConfig("test", ",", true, new List<ValidationConfig>
        {
            new("Id", "int", 0, false, null, null, false, [], [], "Invalid Id") { Name = "Id" },
            new("Name", "string", 1, false, null, null, false, [], [], "Invalid Name") { Name = "Name" },
            new("Age", "int", 2, false, null, null, false, [], [], "Invalid Age") { Name = "Age" }
        });
        var line = "Id,Name,Age";
        var headerValidator = new HeaderValidator();

        // Act
        var results = headerValidator.Validate(1, line, fileConfig);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_EmptyHeader_ReturnsError()
    {
        // Arrange
        var fileConfig = new FileConfig("test", ",", true, new List<ValidationConfig>
        {
            new("Id", "int", 0, false, null, null, false, [], [], "Invalid Id") { Name = "Id" },
            new("Name", "string", 1, false, null, null, false, [], [], "Invalid Name") { Name = "Name" },
            new("Age", "int", 2, false, null, null, false, [], [], "Invalid Age") { Name = "Age" }
        });
        var line = "";
        var headerValidator = new HeaderValidator();

        // Act
        var results = headerValidator.Validate(1, line, fileConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage.Contains("Header is missing 2 column(s)"));
    }
}
