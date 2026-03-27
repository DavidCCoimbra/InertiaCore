using System.Text.Json.Serialization;

namespace InertiaCore.Vite.Models;

/// <summary>
/// A single entry in the Vite manifest.json file.
/// </summary>
public record ManifestEntry
{
    /// <summary>
    /// The hashed output file path.
    /// </summary>
    [JsonPropertyName("file")]
    public required string File { get; init; }

    /// <summary>
    /// The original source file path.
    /// </summary>
    [JsonPropertyName("src")]
    public string? Src { get; init; }

    /// <summary>
    /// Whether this entry is an application entry point.
    /// </summary>
    [JsonPropertyName("isEntry")]
    public bool IsEntry { get; init; }

    /// <summary>
    /// CSS files extracted from this entry.
    /// </summary>
    [JsonPropertyName("css")]
    public string[] Css { get; init; } = [];

    /// <summary>
    /// Static import chunks referenced by this entry.
    /// </summary>
    [JsonPropertyName("imports")]
    public string[] Imports { get; init; } = [];

    /// <summary>
    /// Dynamic import chunks referenced by this entry.
    /// </summary>
    [JsonPropertyName("dynamicImports")]
    public string[] DynamicImports { get; init; } = [];
}
