
public class TransactionConfigLoader
{
    private readonly string _filePath = @"validator/Specifications/Transaction_Spec.json";
    public TransactionConfigLoader()
    {
        
    }
    
    public FileConfig? GetFileConfig()
    {
        var fileConfig = JsonHelperNewtonsoft.ReadFromJsonFileNewtonsoft<FileConfig>(_filePath);
        if (fileConfig == null)
        {
            Console.WriteLine($"Error: Unable to load file config from {_filePath}");
            return null;
        }
        return fileConfig;
    }
}