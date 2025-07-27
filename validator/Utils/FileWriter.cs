using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Newtonsoft.Json;
using validator.Models;

public static class FileWriter
{

    private static void CheckAndCreateDirectory(string directoryPath = "../files/outputs"){
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
    /// <typeparam name="T"></typeparam>
    public static void TryWriteOrAppendToFile<T>(IEnumerable<T> data, string fileName = "transactions.csv")
    {
        CheckAndCreateDirectory();
        if(FileExists(fileName))
        {
            AppendToFile(data, fileName);
        }
        else
        {
            WriteToFile(data, fileName);
        }
    }
    
    /// <summary>
    /// Save the results to a JSON file
    /// This will create a new file in the outputs directory
    /// and write the results to it
    /// The file name will be the same as the input file
    /// but with a .json extension
    /// and the path will be ../files/outputs/
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="results"></param>
    public static void WriteToJson(string filePath, List<LineResult> results)
    {
        string outPutName = filePath.Replace(".csv", ".json").Split("/").Last();
        string outPutPath = Path.Combine("../files/outputs/", outPutName);

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
            //AnsiConsole.MarkupLine($"[green]Wrote results to {outPutPath}[/]");
        }
    }

    static void WriteToFile<T>(IEnumerable<T> data, string fileName)
    {
        // Write to file
        try
        {
            // Append to the file.
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                // Don't write the header again.
                HasHeaderRecord = true,
                Delimiter = "|"
            };
            using (var writer = new StreamWriter($"../files/outputs/{fileName}"))
            using (var csv = new CsvWriter(writer, config))
            {
                var options = new TypeConverterOptions { Formats = ["dd/MM/yyyy"] };
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                csv.WriteRecords(data);
            }
        }
        catch (Exception e)
        {
            // Log error
            Console.WriteLine($"Error writing to file: {e.Message}");
        }
    }

    static void AppendToFile<T>(IEnumerable<T> data, string fileName)
    {
        try{
            // Configure the CSV writer
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                // Don't write the header again.
                HasHeaderRecord = false,
                Delimiter = "|"
            };
            // Append to file
            try
            {    
                using (var stream = File.Open($"../files/outputs/{fileName}", FileMode.Append))
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvWriter(writer, config))
                {
                    var options = new TypeConverterOptions { Formats = ["dd/MM/yyyy"] };
                    csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
                    csv.WriteRecords(data);
                }
            }
            catch( Exception e)
            {
                // Log error
                Console.WriteLine($"Error opening file for appending: {e.Message}");
                return;
            }
        }
        catch(Exception e)
        {
            // Log error
            Console.WriteLine($"Error appending to file: {e.Message}");
        }
    }

    /// <summary>
    /// Check if the file exists.
    /// If the file exists, return true.
    /// If the file does not exist, return false.
    /// This is used to determine if we need to write the header or not.
    /// </summary>
    /// <param name="fileName">The name of the file to check. Default is "transactions.csv".</param>
    /// <returns></returns>
    static bool FileExists(string fileName = "transactions.csv")
    {
        if(File.Exists($"../files/outputs/{fileName}"))
        {
            return true;
        }
        return false;
    }
}