using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Tastemate.Services;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// REST API for the Seerr integration.
/// </summary>
[ApiController]
[Authorize]
[Route("Tastemate/Seerr")]
[Produces(MediaTypeNames.Application.Json)]
public class SeerrController : ControllerBase
{
    private const string UserIdClaim = "Jellyfin-UserId";
    private const int MaxStatusIds = 25;

    private readonly SeerrClient _seerr;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeerrController"/> class.
    /// </summary>
    /// <param name="seerr">Instance of the <see cref="SeerrClient"/> class.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    public SeerrController(SeerrClient seerr, IUserManager userManager)
    {
        _seerr = seerr;
        _userManager = userManager;
    }

    /// <summary>
    /// Gets the Seerr settings. Administrators only.
    /// </summary>
    /// <returns>The settings without the API key.</returns>
    [HttpGet("Settings")]
    public ActionResult<SeerrSettingsDto> GetSettings()
    {
        if (!User.IsInRole("Administrator"))
        {
            return Forbid();
        }

        var config = Plugin.Instance?.Configuration;
        return Ok(new SeerrSettingsDto(config?.SeerrAddress ?? string.Empty, !string.IsNullOrEmpty(config?.SeerrApiKey)));
    }

    /// <summary>
    /// Changes the Seerr settings after checking that they work. Administrators only.
    /// </summary>
    /// <param name="request">The settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stored settings without the API key.</returns>
    [HttpPut("Settings")]
    public async Task<ActionResult<SeerrSettingsDto>> SetSettings([FromBody] SeerrSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Administrator"))
        {
            return Forbid();
        }

        var plugin = Plugin.Instance;
        if (plugin is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var config = plugin.Configuration;
        var address = request.Address?.Trim() ?? string.Empty;
        if (address.Length == 0)
        {
            config.SeerrAddress = string.Empty;
            config.SeerrApiKey = string.Empty;
            plugin.SaveConfiguration();
            return Ok(new SeerrSettingsDto(string.Empty, false));
        }

        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            return BadRequest("Address must start with http:// or https://.");
        }

        var apiKey = string.IsNullOrWhiteSpace(request.ApiKey) ? config.SeerrApiKey : request.ApiKey.Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            return BadRequest("An API key is required.");
        }

        var test = await _seerr.TestAsync(address, apiKey, cancellationToken).ConfigureAwait(false);
        if (!test.Success)
        {
            return BadRequest(test.Message);
        }

        config.SeerrAddress = address.TrimEnd('/');
        config.SeerrApiKey = apiKey;
        plugin.SaveConfiguration();
        return Ok(new SeerrSettingsDto(config.SeerrAddress, true));
    }

    /// <summary>
    /// Gets the Seerr state of movies.
    /// </summary>
    /// <param name="tmdbIds">Comma separated TMDb ids, at most 25.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether Seerr is configured and the state per TMDb id.</returns>
    [HttpGet("Status")]
    public async Task<ActionResult<SeerrStatusResponse>> GetStatus([FromQuery] string? tmdbIds, CancellationToken cancellationToken)
    {
        var states = new Dictionary<string, string>();
        if (!_seerr.IsConfigured)
        {
            return Ok(new SeerrStatusResponse(false, states));
        }

        var ids = (tmdbIds ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(i => i.Length <= 9 && i.All(char.IsAsciiDigit))
            .Distinct(StringComparer.Ordinal)
            .Take(MaxStatusIds)
            .ToList();
        var results = await Task.WhenAll(ids.Select(i => _seerr.GetStateAsync(int.Parse(i, CultureInfo.InvariantCulture), cancellationToken))).ConfigureAwait(false);
        for (var i = 0; i < ids.Count; i++)
        {
            states[ids[i]] = ToName(results[i]);
        }

        return Ok(new SeerrStatusResponse(true, states));
    }

    /// <summary>
    /// Requests a movie in Seerr as the caller's linked Seerr user.
    /// </summary>
    /// <param name="request">The movie.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new state of the movie.</returns>
    [HttpPost("Request")]
    public async Task<ActionResult<SeerrStatusResponse>> RequestMovie([FromBody] SeerrRequestBody request, CancellationToken cancellationToken)
    {
        if (!_seerr.IsConfigured)
        {
            return BadRequest("Seerr is not configured.");
        }

        if (string.IsNullOrEmpty(request.TmdbId) || request.TmdbId.Length > 9 || !request.TmdbId.All(char.IsAsciiDigit))
        {
            return BadRequest("Invalid TMDb id.");
        }

        if (!Guid.TryParse(User.FindFirst(UserIdClaim)?.Value, out var userId) || userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var seerrUserId = await _seerr.FindUserAsync(userId, _userManager.GetUserById(userId)?.Username, cancellationToken).ConfigureAwait(false);
        if (seerrUserId is null)
        {
            return BadRequest("No Seerr account is linked to your Jellyfin user. Sign in to Seerr with your Jellyfin account once, then try again.");
        }

        var tmdbId = int.Parse(request.TmdbId, CultureInfo.InvariantCulture);
        var result = await _seerr.RequestMovieAsync(tmdbId, seerrUserId.Value, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        var state = await _seerr.GetStateAsync(tmdbId, cancellationToken).ConfigureAwait(false);
        var name = ToName(state == SeerrMovieState.None ? SeerrMovieState.Pending : state);
        return Ok(new SeerrStatusResponse(true, new Dictionary<string, string> { [request.TmdbId] = name }));
    }

    private static string ToName(SeerrMovieState state)
    {
        return state switch
        {
            SeerrMovieState.Pending => "pending",
            SeerrMovieState.Processing => "processing",
            SeerrMovieState.PartiallyAvailable => "partial",
            SeerrMovieState.Available => "available",
            _ => "none"
        };
    }
}
