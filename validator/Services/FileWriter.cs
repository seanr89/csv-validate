namespace validator.Services;

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using validator.Models;
using validator.Services.Interfaces;

/// <summary>
/// Service responsible for writing and appending validation results to CSV and JSON files.
/// Configurable delimiter, date formats, and output directories with structured error logging.
/// </summary>
public class FileWriter : IFileWriter
{
    private readonly ILogger<FileWriter> _logger;

    public string DefaultOutputDirectory { get; set; } = "../files/outputs";

    public FileWriter(ILogger<FileWriter>? logger = null)
    {
        _logger = logger ?? NullLogger<FileWriter>.Instance;
    }

    /// <summary>
    /// Try to write to a file; if the file exists and has content, append to it without header.
    /// If the file does not exist or is empty, create it and write with header.
    /// </summary>
    public void TryWriteOrAppendToFile<T>(
        IEnumerable<T> data,
        string fileName = "transactions.csv",
        string? outputDirectory = null,
        string delimiter = "|",
        string dateFormat = "dd/MM/yyyy")
    {
        string directoryPath = outputDirectory ?? DefaultOutputDirectory;
        CheckAndCreateDirectory(directoryPath);

        if (FileExists(directoryPath, fileName))
        {
            AppendToFile(data, fileName, delimiter: delimiter, dateFormat: dateFormat, outputDirectory: directoryPath);
        }
        else
        {
            WriteToFile(data, fileName, writeHeader: true, delimiter: delimiter, dateFormat: dateFormat, outputDirectory: directoryPath);
        }
    }

    /// <summary>
    /// Save the results to a JSON file.
    /// Creates the directory if needed and writes indented JSON results.
    /// </summary>
    public void WriteToJson(string filePath, List<LineResult> results, string? outputDirectory = null)
    {
        string directoryPath = outputDirectory ?? DefaultOutputDirectory;
        CheckAndCreateDirectory(directoryPath);
        string fileName = Path.GetFileName(filePath.Replace('\\', '/'));
        string outPutName = Path.ChangeExtension(fileName, ".json");
        string outPutPath = Path.Combine(directoryPath, outPutName);

        try
        {
            using var sw = new StringWriter();
            using (var writer = new JsonTextWriter(sw)
            {
                Formatting = Formatting.Indented,
                Indentation = 4,
                IndentChar = ' '
            })
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, results);
            }
            string json = sw.ToString();

            using var file = File.CreateText(outPutPath);
            file.WriteLine(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing JSON results to '{Path}'", outPutPath);
            throw;
        }
    }

    /// <summary>
    /// Writes the data to a file. Overwrites or creates the file with optional header.
    /// </summary>
    public void WriteToFile<T>(
        IEnumerable<T> data,
        string fileName,
        bool writeHeader = true,
        string delimiter = "|",
        string dateFormat = "dd/MM/yyyy",
        string? outputDirectory = null)
    {
        WriteRecordsInternal(data, fileName, append: false, writeHeader: writeHeader, delimiter: delimiter, dateFormat: dateFormat, outputDirectory: outputDirectory);
    }

    /// <summary>
    /// Appends the data to an existing file without headers.
    /// </summary>
    public void AppendToFile<T>(
        IEnumerable<T> data,
        string fileName,
        string delimiter = "|",
        string dateFormat = "dd/MM/yyyy",
        string? outputDirectory = null)
    {
        WriteRecordsInternal(data, fileName, append: true, writeHeader: false, delimiter: delimiter, dateFormat: dateFormat, outputDirectory: outputDirectory);
    }

    private void WriteRecordsInternal<T>(
        IEnumerable<T> data,
        string fileName,
        bool append,
        bool writeHeader,
        string delimiter,
        string dateFormat,
        string? outputDirectory)
    {
        var directoryPath = outputDirectory ?? DefaultOutputDirectory;
        CheckAndCreateDirectory(directoryPath);
        var fullPath = Path.Combine(directoryPath, fileName);

        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = writeHeader,
                Delimiter = delimiter
            };

            var mode = append ? FileMode.Append : FileMode.Create;
            using var stream = File.Open(fullPath, mode, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream);
            using var csv = new CsvWriter(writer, config);

            var options = new TypeConverterOptions { Formats = [dateFormat] };
            csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
            csv.WriteRecords(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing CSV records to '{Path}' (append: {Append})", fullPath, append);
            throw;
        }
    }

    private static void CheckAndCreateDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    private static bool FileExists(string directoryPath, string fileName = "transactions.csv")
    {
        var path = Path.Combine(directoryPath, fileName);
        return File.Exists(path) && new FileInfo(path).Length > 0;
    }
}
