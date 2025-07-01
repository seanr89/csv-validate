using Newtonsoft.Json;

namespace validator.Utils;

public static class JsonHelperNewtonsoft
{
    /// <summary>
    /// Reads a JSON file from the specified file path and deserializes it to the specified type using Newtonsoft.Json.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON content to.</typeparam>
    /// <param name="filePath">The path to the JSON file.</param>
    /// <returns>The deserialized object of type T, or the default value of T if an error occurs or the file is not found.</returns>
    public static T? ReadFromJsonFileNewtonsoft<T>(string filePath) where T : class?
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return default;
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return default;
            }

            string jsonString = File.ReadAllText(filePath);
            T? result = JsonConvert.DeserializeObject<T>(jsonString);
            return result;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing JSON file at {filePath} (Newtonsoft.Json): {ex.Message}");
            return default;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error reading file at {filePath}: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// Asynchronously reads a JSON file from the specified file path and deserializes it to the specified type using Newtonsoft.Json.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON content to.</typeparam>
    /// <param name="filePath">The path to the JSON file.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized object of type T,
    /// or the default value of T if an error occurs or the file is not found.</returns>
    public static async Task<T?> ReadFromJsonFileNewtonsoftAsync<T>(string filePath) where T : class?
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return default;
            }

            using StreamReader file = File.OpenText(filePath);
            using JsonTextReader reader = new(file);
            JsonSerializer serializer = new();
            T? result = await Task.Run(() => serializer.Deserialize<T>(reader));
            return result;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing JSON file at {filePath} (Newtonsoft.Json): {ex.Message}");
            return default;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error reading file at {filePath}: {ex.Message}");
            return default;
        }
    }
}