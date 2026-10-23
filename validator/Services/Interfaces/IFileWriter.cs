namespace validator.Services.Interfaces;

using validator.Models;

/// <summary>
/// Service interface for writing validation results to CSV and JSON output files.
/// </summary>
public interface IFileWriter
{
    /// <summary>
    /// Gets or sets the default output directory for written files.
    /// </summary>
    string DefaultOutputDirectory { get; set; }

    /// <summary>
    /// Try to write to a file; if the file exists and has content, append to it without header.
    /// If the file does not exist or is empty, create it and write with header.
    /// </summary>
    void TryWriteOrAppendToFile<T>(
        IEnumerable<T> data,
        string fileName = "transactions.csv",
        string? outputDirectory = null,
        string delimiter = "|",
        string dateFormat = "dd/MM/yyyy");

    /// <summary>
    /// Writes the data to a file. Overwrites or creates the file.
    /// </summary>
    void WriteToFile<T>(
        IEnumerable<T> data,
        string fileName,
        bool writeHeader = true,
        string delimiter = "|",
        string dateFormat = "dd/MM/yyyy",
        string? outputDirectory = null);

    /// <summary>
    /// Appends the data to an existing file without writing headers.
    /// </summary>
    void AppendToFile<T>(
        IEnumerable<T> data,
        string fileName,
        string delimiter = "|",
        string dateFormat = "dd/MM/yyyy",
        string? outputDirectory = null);

    /// <summary>
    /// Saves line validation results to a formatted JSON file in the target directory.
    /// </summary>
    void WriteToJson(
        string filePath,
        List<LineResult> results,
        string? outputDirectory = null);
}
