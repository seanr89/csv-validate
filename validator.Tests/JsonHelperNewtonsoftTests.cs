using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using validator.Utils;

public class JsonHelperNewtonsoftTests
{
    private class TestClass
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    [Fact]
    public void ReadFromJsonFileNewtonsoft_ReturnsObject_WhenFileIsValid()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "{\"Name\":\"Test\",\"Value\":42}");

        // Act
        var result = JsonHelperNewtonsoft.ReadFromJsonFileNewtonsoft<TestClass>(tempFile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result!.Name);
        Assert.Equal(42, result.Value);

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public void ReadFromJsonFileNewtonsoft_ReturnsNull_WhenFileNotFound()
    {
        // Act
        var result = JsonHelperNewtonsoft.ReadFromJsonFileNewtonsoft<TestClass>("nonexistent.json");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ReadFromJsonFileNewtonsoft_ReturnsNull_WhenJsonInvalid()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "not a json");

        // Act
        var result = JsonHelperNewtonsoft.ReadFromJsonFileNewtonsoft<TestClass>(tempFile);

        // Assert
        Assert.Null(result);

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public async Task ReadFromJsonFileNewtonsoftAsync_ReturnsObject_WhenFileIsValid()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "{\"Name\":\"AsyncTest\",\"Value\":99}");

        // Act
        var result = await JsonHelperNewtonsoft.ReadFromJsonFileNewtonsoftAsync<TestClass>(tempFile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("AsyncTest", result!.Name);
        Assert.Equal(99, result.Value);

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public async Task ReadFromJsonFileNewtonsoftAsync_ReturnsNull_WhenFileNotFound()
    {
        // Act
        var result = await JsonHelperNewtonsoft.ReadFromJsonFileNewtonsoftAsync<TestClass>("nonexistent.json");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadFromJsonFileNewtonsoftAsync_ReturnsNull_WhenJsonInvalid()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "not a json");

        // Act
        var result = await JsonHelperNewtonsoft.ReadFromJsonFileNewtonsoftAsync<TestClass>(tempFile);

        // Assert
        Assert.Null(result);

        // Cleanup
        File.Delete(tempFile);
    }
}
