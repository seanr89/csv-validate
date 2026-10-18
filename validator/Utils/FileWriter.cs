using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Newtonsoft.Json;
using validator.Models;

public static class FileWriter
{

    public static string DefaultOutputDirectory { get; set; } = "../files/outputs";

    private static void CheckAndCreateDirectory(string directoryPath)
    {
        if(!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// Try to write to a file, if the file exists, append to it.
    /// If the file does not exist, create it and write to it.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="fileName"></param>
    /// <param name="outputDirectory"></param>
    /// <typeparam name="T"></typeparam>
    public static void TryWriteOrAppendToFile<T>(IEnumerable<T> data, string fileName = "transactions.csv", string? outputDirectory = null)
    {
        string directoryPath = outputDirectory ?? DefaultOutputDirectory;
        CheckAndCreateDirectory(directoryPath);
        if(FileExists(directoryPath, fileName))
        {
            AppendToFile(data, fileName, directoryPath: directoryPath);
        }
        else
        {
            WriteToFile(data, fileName, directoryPath: directoryPath);
        }
    }
    
    /// <summary>
    /// Save the results to a JSON file
    /// This will create a new file in the outputs directory
    /// and write the results to it
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="results"></param>
    /// <param name="outputDirectory"></param>
    public static void WriteToJson(string filePath, List<LineResult> results, string? outputDirectory = null)
    {
        string directoryPath = outputDirectory ?? DefaultOutputDirectory;
        CheckAndCreateDirectory(directoryPath);
        string fileName = Path.GetFileName(filePath.Replace('\\', '/'));
        string outPutName = Path.ChangeExtension(fileName, ".json");
        string outPutPath = Path.Combine(directoryPath, outPutName);

        // create JSON configuration for newtonsoft
        using StringWriter sw = new StringWriter();
        using JsonTextWriter writer = new JsonTextWriter(sw)
        {
            Formatting = Formatting.Indented,
            Indentation = 4,
            IndentChar = ' '
        };
        JsonSerializer serializer = new JsonSerializer();
        serializer.Serialize(writer, results);
        string json = sw.ToString();

        // write the json to a file
        using (StreamWriter file = File.CreateText(outPutPath))
        {
            file.WriteLine(json);
            file.Flush();
            file.Close();
        }
    }

    /// <summary>
    /// Write the data to a file. If the file does not exist, create it and write to it.
    /// </summary>
    static void WriteToFile<T>(IEnumerable<T> data, string fileName, bool writeHeader = true, string delimiter = "|", string? directoryPath = null)
    {
        directoryPath ??= DefaultOutputDirectory;
        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = writeHeader,
                Delimiter = delimiter
            };
            var fullPath = Path.Combine(directoryPath, fileName);
            using (var writer = new StreamWriter(fullPath))
            using (var csv = new CsvWriter(writer, config))
            {
                var options = new TypeConverterOptions { Formats = ["dd/MM/yyyy"] };
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.WriteRecords(data);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error writing to file: {e.Message}");
        }
    }

    /// <summary>
    /// Append the data to an existing file. The header is never written, as the file already has one.
    /// </summary>
    static void AppendToFile<T>(IEnumerable<T> data, string fileName, string delimiter = "|", string? directoryPath = null)
    {
        directoryPath ??= DefaultOutputDirectory;
        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = delimiter
            };
            try
            {    
                var fullPath = Path.Combine(directoryPath, fileName);
                using (var stream = File.Open(fullPath, FileMode.Append))
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvWriter(writer, config))
                {
                    var options = new TypeConverterOptions { Formats = ["dd/MM/yyyy"] };
                    csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                    csv.WriteRecords(data);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error opening file for appending: {e.Message}");
                return;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error appending to file: {e.Message}");
        }
    }

    /// <summary>
    /// Check if the file exists and has content.
    /// </summary>
    static bool FileExists(string directoryPath, string fileName = "transactions.csv")
    {
        var path = Path.Combine(directoryPath, fileName);
        return File.Exists(path) && new FileInfo(path).Length > 0;
    }
}