

using Microsoft.Extensions.Logging;

// Dedicated to the type validation logic
// This class is responsible for validating different types of data
// such as int, string, bool, DateTime, double, and decimal.
// It uses the ILogger interface for logging validation results and errors.
public class TypeValidator
{
    private readonly ILogger<TypeValidator> _logger;

    public TypeValidator(ILogger<TypeValidator> logger)
    {
        _logger = logger;
    }


    /// <summary>
    /// Validates the type of a given value against an expected type.
    /// This method checks if the value can be converted to the expected type
    /// and returns a boolean indicating the validity.
    /// It also accepts an array of formats for additional validation.
    /// The formats parameter can be used to specify the expected format of the value.
    /// For example, if the expected type is "DateTime", you can specify formats like "MM/dd/yyyy" or "yyyy-MM-dd".
    /// The method will log the validation process and return true if the value is valid for the expected type,
    /// or false if it is not valid.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="expectedType"></param>
    /// <param name="formats"></param>
    /// <returns></returns>
    public bool ValidateType(string value, string expectedType, string[] formats)
    {
        _logger.LogInformation($"Validating type for value: {value} against expected type: {expectedType}");

        // Perform the validation logic here
        // For example, you can use reflection or a switch statement to check the type
        switch (expectedType.ToLower())
        {
            case "int":
                return ValidateInt(value);
            case "string":
                return ValidateString(value);
            case "bool":
                return ValidateBool(value);
            case "datetime":
                return ValidateDateTime(value, formats);
            case "double":
                return ValidateDouble(value);
            case "decimal":
                return ValidateDecimal(value);
            default:
                _logger.LogWarning($"Unknown expected type: {expectedType}");
                return false;
        }
    }

    public bool ValidateBool(string value)
    {
        if (bool.TryParse(value, out bool result))
        {
            return true;
        }
        else
        {
            _logger.LogWarning($"Invalid boolean value: {value}");
            return false;
        }
    }

    public bool ValidateDateTime(string value, string[] formats)
    {
        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value, format, null, System.Globalization.DateTimeStyles.None, out _))
            {
                return true;
            }
        }

        _logger.LogWarning($"Invalid DateTime value: {value}");
        return false;
    }

    public bool ValidateDouble(string value)
    {
        if (double.TryParse(value, out double result))
        {
            return true;
        }
        else
        {
            _logger.LogWarning($"Invalid double value: {value}");
            return false;
        }
    }
    public bool ValidateDecimal(string value)
    {
        if (decimal.TryParse(value, out decimal result))
        {
            return true;
        }
        else
        {
            _logger.LogWarning($"Invalid decimal value: {value}");
            return false;
        }
    }
    public bool ValidateInt(string value)
    {
        if (int.TryParse(value, out int result))
        {
            return true;
        }
        else
        {
            _logger.LogWarning($"Invalid int value: {value}");
            return false;
        }
    }
    public bool ValidateString(string value)
    {
        // All values are valid strings
        return true;
    }
}