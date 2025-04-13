
using System.Text.Json;

public class ValidationConfigLoader
{
    private readonly string _configFilePath;

    public ValidationConfigLoader(string configFilePath)
    {
        _configFilePath = configFilePath;
    }

    public ValidationConfig Load()
    {
        if (!File.Exists(_configFilePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {_configFilePath}");
        }

        var json = File.ReadAllText(_configFilePath);
        return JsonSerializer.Deserialize<ValidationConfig>(json) ?? new ValidationConfig();
    }
}