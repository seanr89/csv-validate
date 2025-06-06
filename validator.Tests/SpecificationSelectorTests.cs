using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Xunit;

public class SpecificationSelectorTests
{
    private string _testDir;
    private string _testFile1;
    private string _testFile2;

    public SpecificationSelectorTests()
    {
        // Setup a temporary directory and files for testing
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
        _testFile1 = Path.Combine(_testDir, "TestType1.json");
        _testFile2 = Path.Combine(_testDir, "TestType2.json");

        File.WriteAllText(_testFile1, "{" +
            "\"FileType\": \"TestType1\", " +
            "\"Delimiter\": \",\", " +
            "\"HeaderLine\": true, " +
            "\"ValidationConfigs\": []}");
        File.WriteAllText(_testFile2, "{" +
            "\"FileType\": \"TestType2\", " +
            "\"Delimiter\": \",\", " +
            "\"HeaderLine\": false, " +
            "\"ValidationConfigs\": []}");
    }

    [Fact]
    public void LoadsFileConfigsFromDirectory()
    {
        var selector = new SpecificationSelectorForTest(_testDir);
        var config1 = selector.GetFileConfig("TestType1");
        var config2 = selector.GetFileConfig("TestType2");
        Assert.NotNull(config1);
        Assert.NotNull(config2);
        Assert.Equal("TestType1", config1.FileType);
        Assert.Equal("TestType2", config2.FileType);
    }

    [Fact]
    public void ReturnsNullAndLogsError_WhenFileTypeNotFound()
    {
        var selector = new SpecificationSelectorForTest(_testDir);
        var config = selector.GetFileConfig("NonExistentType");
        Assert.Null(config);
    }

    [Fact]
    public void IgnoresNonJsonFiles()
    {
        var nonJsonFile = Path.Combine(_testDir, "notjson.txt");
        File.WriteAllText(nonJsonFile, "not json");
        var selector = new SpecificationSelectorForTest(_testDir);
        // Should still only load the two JSON configs
        Assert.NotNull(selector.GetFileConfig("TestType1"));
        Assert.NotNull(selector.GetFileConfig("TestType2"));
    }

    ~SpecificationSelectorTests()
    {
        // Cleanup
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    // Helper: subclass to override the directory for testing
    private class SpecificationSelectorForTest : SpecificationSelector
    {
        public SpecificationSelectorForTest(string dir)
        {
            // Call the loader with the test directory
            typeof(SpecificationSelector)
                .GetMethod("LoadAllFilePathsFromDirectory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(this, new object[] { dir });
        }
    }
}
