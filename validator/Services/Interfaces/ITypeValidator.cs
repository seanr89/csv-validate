namespace validator.Services.Interfaces;

public interface ITypeValidator
{
    bool ValidateType(string value, string expectedType, string[] formats);
    bool ValidateBool(string value);
    bool ValidateDateTime(string value, string[] formats);
    bool ValidateDouble(string value);
    bool ValidateDecimal(string value);
    bool ValidateInt(string value);
    bool ValidateString(string value);
}
