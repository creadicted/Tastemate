namespace Jellyfin.Plugin.Tastemate.Services;

/// <summary>
/// The state of a movie in Seerr.
/// </summary>
public enum SeerrMovieState
{
    /// <summary>Not requested, or the state could not be read.</summary>
    None,

    /// <summary>Requested and waiting for approval.</summary>
    Pending,

    /// <summary>Approved and being downloaded.</summary>
    Processing,

    /// <summary>Partially available.</summary>
    PartiallyAvailable,

    /// <summary>Available.</summary>
    Available
}
