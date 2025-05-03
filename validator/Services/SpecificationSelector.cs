

using Newtonsoft.Json;

public class SpecificationSelector
{
    List<FileConfig> _fileConfigs = [];
    
    public SpecificationSelector()
    {
        LoadAllFilePathsFromDirectory("Specifications");
    }

    public FileConfig? GetFileConfig(string fileType)
    {
        Console.WriteLine($"Getting file config for {fileType}");
        // Find the file config based on the file type
        var fileConfig = _fileConfigs.FirstOrDefault(f => f.FileType.Equals(fileType, StringComparison.OrdinalIgnoreCase));
        if (fileConfig == null)
        {
            Console.WriteLine($"Error: Unable to find file config for {fileType}");
            return null;
        }
        return fileConfig;
    }

    void LoadAllFilePathsFromDirectory(string directoryPath)
    {
        Console.WriteLine($"Loading file paths from directory: {directoryPath}");
        // Get all files in the directory
        var files = Directory.GetFiles(directoryPath);

        // Filter files based on extension
        foreach (var file in files)
        {
            if (file.EndsWith(".json"))
            {
                Console.WriteLine($"Loading file: {file}");
                // Load the file config
                var fileConfig = LoadFileConfig(file);
                // only add if the file config is not null
                if (fileConfig == null)
                {
                    Console.WriteLine($"Error: Unable to load file config from {file}");
                    continue;
                }
                _fileConfigs.Add(fileConfig);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    FileConfig? LoadFileConfig(string filePath)
    {
        // Load the file config from the JSON file
        var jsonString = File.ReadAllText(filePath);
        var fileConfig = JsonConvert.DeserializeObject<FileConfig>(jsonString);
        return fileConfig;
    }
}