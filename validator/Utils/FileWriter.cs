using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

public static class FileWriter
{

    public static void CheckAndCreateDirectory(string directoryPath = "../files/output"){
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

    static void WriteToFile<T>(IEnumerable<T> data, string fileName)
    {
        // Write to file
        try{
            // Append to the file.
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                // Don't write the header again.
                HasHeaderRecord = true,
                Delimiter = "|"
            };
            using (var writer = new StreamWriter($"./files/{fileName}"))
            using (var csv = new CsvWriter(writer, config))
            {
                var options = new TypeConverterOptions { Formats = ["dd/MM/yyyy"] };
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);          
                csv.WriteRecords(data);
            }
        }
        catch(Exception e)
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
            using (var stream = File.Open($"../files/output/{fileName}", FileMode.Append))
            using (var writer = new StreamWriter(stream))
            using (var csv = new CsvWriter(writer, config))
            {
                var options = new TypeConverterOptions { Formats = ["dd/MM/yyyy"] };
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);       
                csv.WriteRecords(data);
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
        if(File.Exists($"../files/output/{fileName}"))
        {
            return true;
        }
        return false;
    }
}