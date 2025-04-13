
public record LineResult(int lineCount, bool valid, string? errorMessage)
{
    public int LineCount { get; } = lineCount;
    public bool Valid { get; } = valid;
    public string? ErrorMessage { get; } = errorMessage;

    public override string ToString()
    {
        return $"LineCount: {LineCount}, Valid: {Valid}, ErrorMessage: {ErrorMessage}";
    }
}