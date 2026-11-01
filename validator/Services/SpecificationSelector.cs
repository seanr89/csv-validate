using validator.Models;
using validator.Services.Interfaces;
using Newtonsoft.Json;

namespace validator.Services;

public class SpecificationSelector : ISpecificationSelector
{
    readonly List<FileConfig> _fileConfigs = [];
    
    /// <summary>
    /// SpecificationSelector is responsible for loading file configurations from a directory.
    /// It scans the specified directory for JSON files, loads their configurations,
    /// and provides methods to retrieve file configurations based on file type.
    /// </summary>
    public SpecificationSelector()
        : this(ResolveSpecificationsDirectory())
    {
    }

    public SpecificationSelector(string directoryPath)
    {
        LoadAllFilePathsFromDirectory(directoryPath);
    }

    /// <summary>
    /// Resolves the Specifications directory, checking AppContext.BaseDirectory, current working directory,
    /// and parent directories so it works whether running from validator/, the repository root, or test runners.
    /// </summary>
    public static string ResolveSpecificationsDirectory(string? preferredPath = null)
    {
        if (!string.IsNullOrWhiteSpace(preferredPath) && Directory.Exists(preferredPath))
        {
            return Path.GetFullPath(preferredPath);
        }

        // 1. Check AppContext.BaseDirectory/Specifications (copied during build)
        var baseDirSpec = Path.Combine(AppContext.BaseDirectory, "Specifications");
        if (Directory.Exists(baseDirSpec))
        {
            return baseDirSpec;
        }

        // 2. Check current working directory/Specifications
        var currentDirSpec = Path.Combine(Directory.GetCurrentDirectory(), "Specifications");
        if (Directory.Exists(currentDirSpec))
        {
            return currentDirSpec;
        }

        // 3. Search up parent directories from AppContext.BaseDirectory
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "validator", "Specifications");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(dir.FullName, "Specifications");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return "Specifications";
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
        var fileConfig = _fileConfigs.FirstOrDefault(f =>
            !string.IsNullOrWhiteSpace(f.FileType) &&
            f.FileType.Equals(fileType, StringComparison.OrdinalIgnoreCase));
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
    /// If the file cannot be read, deserialized, or fails validation, it logs the issue and returns null.
    /// </summary>
    /// <param name="filePath">filePathing of the expected file to loaded</param>
    /// <returns>nullable FileConfig object model</returns>
    static FileConfig? LoadFileConfig(string filePath)
    {
        try
        {
            // Load the file config from the JSON file
            var jsonString = File.ReadAllText(filePath);
            var fileConfig = JsonConvert.DeserializeObject<FileConfig>(jsonString);
            if (!ValidateFileConfig(filePath, fileConfig))
            {
                return null;
            }
            return fileConfig;
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            Console.WriteLine($"Error loading specification file '{filePath}': {ex.Message}");
            return null;
        }
    }

    static bool ValidateFileConfig(string filePath, FileConfig? fileConfig)
    {
        if (fileConfig == null)
        {
            Console.WriteLine($"Error in specification file '{filePath}': deserialized config is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(fileConfig.FileType))
        {
            Console.WriteLine($"Error in specification file '{filePath}': 'fileType' is required.");
            return false;
        }

        if (string.IsNullOrEmpty(fileConfig.Delimiter))
        {
            Console.WriteLine($"Error in specification file '{filePath}': 'delimiter' is required.");
            return false;
        }

        if (fileConfig.ValidationConfigs == null)
        {
            Console.WriteLine($"Error in specification file '{filePath}': 'validationConfigs' is required.");
            return false;
        }

        for (int i = 0; i < fileConfig.ValidationConfigs.Count; i++)
        {
            var col = fileConfig.ValidationConfigs[i];
            if (col == null)
            {
                Console.WriteLine($"Error in specification file '{filePath}': validationConfig entry {i} is null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(col.Name))
            {
                Console.WriteLine($"Error in specification file '{filePath}': column at index {col.Index} (entry {i}) is missing 'name'.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(col.Type))
            {
                Console.WriteLine($"Error in specification file '{filePath}': column '{col.Name}' at index {col.Index} is missing 'type'.");
                return false;
            }
        }

        return true;
    }
}