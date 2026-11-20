namespace Jellyfin.Plugin.Tastemate.Services;

/// <summary>
/// The outcome of a Seerr call.
/// </summary>
/// <param name="Success">Whether the call succeeded.</param>
/// <param name="Message">An error message when it did not.</param>
public record SeerrResult(bool Success, string? Message);
