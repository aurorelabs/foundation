using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Extensions;

namespace Aurore.Foundation.Core.Configuration;

/// <summary>
/// The JSON conventions shared by every part of the foundation: camelCase, enums as strings, cycles ignored, compact
/// output.
/// </summary>
public static class JsonSerializerConfiguration
{
    /// <summary>Returns a new options object with the shared conventions applied.</summary>
    public static JsonSerializerOptions Default()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        Configure(options);

        return options;
    }

    /// <summary>
    /// Applies the shared conventions to an existing options object, for example the one ASP.NET Core uses.
    /// </summary>
    /// <param name="options">The options to change.</param>
    public static void Configure(JsonSerializerOptions options)
    {
        options.AllowTrailingCommas = true;
        options.PropertyNameCaseInsensitive = true;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.WriteIndented = false;
        options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        options.Converters.Clear();
        options.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>
    /// Serializes the object with the shared conventions; the optional action adjusts them for this call.
    /// </summary>
    /// <param name="source">The object to serialize.</param>
    /// <param name="optionsAction">Adjusts the options for this call only.</param>
    /// <returns>The JSON text.</returns>
    public static string Serialize(this object source, Action<JsonSerializerOptions>? optionsAction = null)
    {
        return JsonSerializer.Serialize(source, BuildOptions(optionsAction));
    }

    /// <summary>Writes the object to a file as indented JSON unless the options action says otherwise.</summary>
    /// <param name="source">The object to serialize.</param>
    /// <param name="filePath">The file to write. WSL and Windows paths are converted to the style of the running system.</param>
    /// <param name="optionsAction">Adjusts the options for this call only.</param>
    public static void SerializeToFile(this object source, string filePath, Action<JsonSerializerOptions>? optionsAction = null)
    {
        filePath = Path.NormalizeRuntimePath(filePath);

        File.WriteAllText(filePath, source.Serialize(optionsAction is null ? x => x.WriteIndented = true : optionsAction));
    }

    /// <summary>Writes the value to the stream with the shared conventions.</summary>
    /// <param name="source">The stream to write to.</param>
    /// <param name="target">The value to serialize.</param>
    /// <param name="optionsAction">Adjusts the options for this call only.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    public static async Task SerializeAsync<T>(this Stream source, T target, Action<JsonSerializerOptions>? optionsAction = null, CancellationToken cancellationToken = default)
    {
        await JsonSerializer.SerializeAsync(source, target, BuildOptions(optionsAction), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads a value from JSON text with the shared conventions.</summary>
    /// <param name="source">The JSON text.</param>
    /// <param name="optionsAction">Adjusts the options for this call only.</param>
    /// <returns>The deserialized value.</returns>
    public static T Deserialize<T>(this string source, Action<JsonSerializerOptions>? optionsAction = null)
    {
        return JsonSerializer.Deserialize<T>(source, BuildOptions(optionsAction))!;
    }

    /// <summary>Reads a value from a stream with the shared conventions.</summary>
    /// <param name="source">The stream to read from.</param>
    /// <param name="optionsAction">Adjusts the options for this call only.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The deserialized value.</returns>
    public static async ValueTask<T> DeserializeAsync<T>(this Stream source, Action<JsonSerializerOptions>? optionsAction = null, CancellationToken cancellationToken = default)
    {
        return (await JsonSerializer.DeserializeAsync<T>(source, BuildOptions(optionsAction), cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Reads a value from a JSON file. A missing file throws, or returns the default value when throwOnError is false.
    /// </summary>
    /// <param name="filename">The file to read. WSL and Windows paths are converted to the style of the running system.</param>
    /// <param name="throwOnError">True to throw when the file is missing; false to return the default value.</param>
    /// <returns>The deserialized value, or the default value when the file is missing and throwOnError is false.</returns>
    public static T? DeserializeFromFile<T>(this string filename, bool throwOnError = true)
    {
        if (File.Exists(filename) is false)
            return throwOnError ? throw new FileNotFoundException($"The file '{filename}' was not found.") : default;

        return File.ReadAllText(filename).Deserialize<T>();
    }

    private static JsonSerializerOptions BuildOptions(Action<JsonSerializerOptions>? optionsAction)
    {
        var options = Default();

        optionsAction?.Invoke(options);

        return options;
    }
}
