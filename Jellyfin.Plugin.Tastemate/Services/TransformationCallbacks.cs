using System;
using System.Globalization;

namespace Jellyfin.Plugin.Tastemate.Services;

/// <summary>
/// Callbacks the File Transformation plugin calls by name when the web client files are served.
/// </summary>
public static class TransformationCallbacks
{
    /// <summary>
    /// Adds the menu script to index.html.
    /// </summary>
    /// <param name="payload">The file.</param>
    /// <returns>The changed file.</returns>
    public static string IndexHtml(PatchRequestPayload payload)
    {
        var contents = payload.Contents ?? string.Empty;
        if (contents.Contains("plugin=\"Tastemate\"", StringComparison.Ordinal))
        {
            return contents;
        }

        // Relative, so it also works when Jellyfin runs under a base URL: /web/ becomes /Tastemate/inject.js
        var version = Plugin.Instance?.Version.ToString() ?? "0";
        var script = string.Create(CultureInfo.InvariantCulture, $"<script plugin=\"Tastemate\" src=\"../Tastemate/inject.js?v={version}\" defer></script>");
        var index = contents.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        return index < 0 ? contents : contents.Insert(index, script);
    }
}
