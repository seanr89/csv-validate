

using Microsoft.Extensions.Logging;

public class TypeValidator
{
    private readonly ILogger<TypeValidator> _logger;

    public TypeValidator(ILogger<TypeValidator> logger)
    {
        _logger = logger;
    }

    public bool ValidateType(string value, string expectedType)
    {
        _logger.LogInformation($"Validating type for value: {value} against expected type: {expectedType}");

        // Perform the validation logic here
        // For example, you can use reflection or a switch statement to check the type
        switch (expectedType.ToLower())
        {
            case "int":
                return int.TryParse(value, out _);
            case "string":
                return true; // All values are valid strings
            case "bool":
                return bool.TryParse(value, out _);
            default:
                _logger.LogWarning($"Unknown expected type: {expectedType}");
                return false;
        }
    }

    RecordResult? ValidateBool(string value)
    {
        RecordResult? result = null;

        return result;
    }
}