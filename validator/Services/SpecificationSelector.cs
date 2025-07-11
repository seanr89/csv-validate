using Newtonsoft.Json;

public class SpecificationSelector : ISpecificationSelector
{
    readonly List<FileConfig> _fileConfigs = [];
    
    /// <summary>
    /// SpecificationSelector is responsible for loading file configurations from a directory.
    /// It scans the specified directory for JSON files, loads their configurations,
    /// and provides methods to retrieve file configurations based on file type.
    /// </summary>
    public SpecificationSelector()
    {
        LoadAllFilePathsFromDirectory("Specifications");
    }

    /// <summary>
    /// Gets the file configuration for a specific file type.
    /// This method searches through the loaded file configurations and returns the one that matches the specified file type.
    /// If no matching file configuration is found, it returns null and logs an error message.
    /// This is used to validate files based on their type.
    /// </summary>
    /// <param name="fileType">string param of said file type</param>
    /// <returns></returns>
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

    /// <summary>
    /// Internal function to load all file paths from a directory
    /// This will load all files in the directory and filter based on the extension
    /// and load the file config
    /// </summary>
    /// <param name="directoryPath"></param>
    void LoadAllFilePathsFromDirectory(string directoryPath)
    {
        Console.WriteLine($"Loading file paths from directory: {directoryPath}");
        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Error: Directory {directoryPath} does not exist.");
            return;
        }
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
    /// Runs the file config loader for a specific file path.
    /// This method reads a JSON file from the specified path, deserializes it into a FileConfig object,
    /// and returns the FileConfig object.
    /// If the file cannot be read or deserialized, it returns null.
    /// </summary>
    /// <param name="filePath">filePathing of the expected file to loaded</param>
    /// <returns>nullable FileConfig object model</returns>
    static FileConfig? LoadFileConfig(string filePath)
    {
        // Load the file config from the JSON file
        var jsonString = File.ReadAllText(filePath);
        var fileConfig = JsonConvert.DeserializeObject<FileConfig>(jsonString);
        return fileConfig;
    }
}