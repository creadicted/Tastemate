namespace Jellyfin.Plugin.Tastemate.Services;

/// <summary>
/// The file contents the File Transformation plugin hands to a callback.
/// </summary>
public class PatchRequestPayload
{
    /// <summary>
    /// Gets or sets the contents of the file.
    /// </summary>
    public string? Contents { get; set; }
}
