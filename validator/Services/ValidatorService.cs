
public class ValidatorService
{
    public ValidatorService()
    {
        
    }

    public List<LineResult> ProcessFileWithConfig(string filePath, FileConfig fileConfig)
    {
        // Load the file
        var lines = File.ReadAllLines(filePath);
        var results = new List<LineResult>();

        // Process each line
        for(int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineCount = i + 1; // Line numbers are 1-based
            results.Add(ProcessLine(lineCount, line, fileConfig));
        }
        // {
        //     var result = ProcessLine(line, fileConfig);
        //     results.Add(result);
        // }

        return results;
    }

    private LineResult ProcessLine(int lineCount, string line, FileConfig fileConfig)
    {
        var result = new LineResult(lineCount, false, string.Empty);
        var fields = line.Split(',');

        // Validate each field based on the config
        for (int i = 0; i < fields.Length; i++)
        {
            
        }

        return result;
    }
}