using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Tastemate.Models;
using Jellyfin.Plugin.Tastemate.Services;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// REST API for topics, rankings and comparisons.
/// </summary>
[ApiController]
[Authorize]
[Route("Tastemate")]
[Produces(MediaTypeNames.Application.Json)]
public class TastemateController : ControllerBase
{
    private const string UserIdClaim = "Jellyfin-UserId";
    private const int MaxTitleLength = 100;
    private const int MaxMovieTitleLength = 200;
    private const string PosterPrefix = "https://image.tmdb.org/";

    private readonly TopStore _store;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IProviderManager _providerManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TastemateController"/> class.
    /// </summary>
    /// <param name="store">Instance of the <see cref="TopStore"/> class.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    public TastemateController(TopStore store, ILibraryManager libraryManager, IUserManager userManager, IProviderManager providerManager)
    {
        _store = store;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _providerManager = providerManager;
    }

    /// <summary>
    /// Searches movies with the server's metadata providers (TMDb), including movies that are not in the library.
    /// </summary>
    /// <param name="query">The movie name.</param>
    /// <param name="year">An optional production year.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching movies that have a TMDb id.</returns>
    [HttpGet("Search")]
    public async Task<ActionResult<IReadOnlyList<SearchResultDto>>> Search([FromQuery] string query, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > MaxMovieTitleLength)
        {
            return BadRequest("Query must be 1-200 characters.");
        }

        var searchQuery = new RemoteSearchQuery<MovieInfo>
        {
            SearchInfo = new MovieInfo { Name = query.Trim(), Year = year }
        };

        var results = await _providerManager.GetRemoteSearchResults<Movie, MovieInfo>(searchQuery, cancellationToken).ConfigureAwait(false);
        var movies = results
            .Select(r => (Result: r, TmdbId: r.GetProviderId(MediaBrowser.Model.Entities.MetadataProvider.Tmdb)))
            .Where(r => !string.IsNullOrEmpty(r.TmdbId) && !string.IsNullOrEmpty(r.Result.Name))
            .GroupBy(r => r.TmdbId)
            .Select(g => g.First())
            .Take(20)
            .Select(r => new SearchResultDto(r.TmdbId!, r.Result.Name, r.Result.ProductionYear, r.Result.ImageUrl))
            .ToList();
        return Ok(movies);
    }

    /// <summary>
    /// Lists all topics.
    /// </summary>
    /// <returns>The topics, newest first.</returns>
    [HttpGet("Topics")]
    public async Task<ActionResult<IReadOnlyList<Topic>>> GetTopics()
    {
        return Ok(await _store.GetTopicsAsync().ConfigureAwait(false));
    }

    /// <summary>
    /// Creates a topic.
    /// </summary>
    /// <param name="request">The topic.</param>
    /// <returns>The created topic.</returns>
    [HttpPost("Topics")]
    public async Task<ActionResult<Topic>> CreateTopic([FromBody] CreateTopicRequest request)
    {
        var title = request.Title?.Trim() ?? string.Empty;
        var maxSize = Plugin.Instance?.Configuration.MaxTopSize ?? 25;
        if (title.Length is 0 or > MaxTitleLength)
        {
            return BadRequest($"Title must be 1-{MaxTitleLength} characters.");
        }

        if (request.Size < 1 || request.Size > maxSize)
        {
            return BadRequest($"Size must be 1-{maxSize}.");
        }

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await _store.CreateTopicAsync(title, request.Size, userId).ConfigureAwait(false));
    }

    /// <summary>
    /// Deletes a topic. Only the creator or an administrator may do this.
    /// </summary>
    /// <param name="topicId">The topic id.</param>
    /// <returns>No content.</returns>
    [HttpDelete("Topics/{topicId}")]
    public async Task<ActionResult> DeleteTopic([FromRoute] Guid topicId)
    {
        var topic = await _store.GetTopicAsync(topicId).ConfigureAwait(false);
        if (topic is null)
        {
            return NotFound();
        }

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (topic.CreatedByUserId != userId && !User.IsInRole("Administrator"))
        {
            return Forbid();
        }

        await _store.DeleteTopicAsync(topicId).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Lists the users who ranked a topic.
    /// </summary>
    /// <param name="topicId">The topic id.</param>
    /// <returns>The users.</returns>
    [HttpGet("Topics/{topicId}/Rankings")]
    public async Task<ActionResult<IReadOnlyList<RankerDto>>> GetRankers([FromRoute] Guid topicId)
    {
        if (await _store.GetTopicAsync(topicId).ConfigureAwait(false) is null)
        {
            return NotFound();
        }

        var rankings = await _store.GetRankingsAsync(topicId).ConfigureAwait(false);
        return Ok(rankings.Select(r => new RankerDto(r.UserId, GetUserName(r.UserId), r.Entries.Count)).ToList());
    }

    /// <summary>
    /// Gets a user's ranking for a topic.
    /// </summary>
    /// <param name="topicId">The topic id.</param>
    /// <param name="userId">The user id.</param>
    /// <returns>The ranking.</returns>
    [HttpGet("Topics/{topicId}/Rankings/{userId}")]
    public async Task<ActionResult<RankingDto>> GetRanking([FromRoute] Guid topicId, [FromRoute] Guid userId)
    {
        var ranking = await _store.GetRankingAsync(topicId, userId).ConfigureAwait(false);
        if (ranking is null)
        {
            return NotFound();
        }

        return Ok(new RankingDto(userId, GetUserName(userId), ranking.Entries.Select(ToDto).ToList()));
    }

    /// <summary>
    /// Replaces the caller's ranking for a topic.
    /// </summary>
    /// <param name="topicId">The topic id.</param>
    /// <param name="request">The ranking.</param>
    /// <returns>The stored ranking.</returns>
    [HttpPut("Topics/{topicId}/Ranking")]
    public async Task<ActionResult<RankingDto>> SetRanking([FromRoute] Guid topicId, [FromBody] SetRankingRequest request)
    {
        var topic = await _store.GetTopicAsync(topicId).ConfigureAwait(false);
        if (topic is null)
        {
            return NotFound();
        }

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var entries = request.Entries ?? [];
        if (entries.Count > topic.Size)
        {
            return BadRequest($"At most {topic.Size} entries allowed.");
        }

        if (entries.Any(e => e.Rank < 1 || e.Rank > topic.Size)
            || entries.Select(e => e.Rank).Distinct().Count() != entries.Count)
        {
            return BadRequest($"Ranks must be unique and between 1 and {topic.Size}.");
        }

        foreach (var entry in entries)
        {
            var error = Validate(entry);
            if (error is not null)
            {
                return BadRequest(error);
            }
        }

        var resolved = entries.Select(Resolve).ToList();
        if (resolved.Select(e => e.Key).Distinct().Count() != resolved.Count)
        {
            return BadRequest("A movie can only be ranked once.");
        }

        var ranking = await _store.SetRankingAsync(topicId, userId, resolved).ConfigureAwait(false);
        return Ok(new RankingDto(userId, GetUserName(userId), ranking.Entries.Select(ToDto).ToList()));
    }

    /// <summary>
    /// Compares the caller's ranking with another user's.
    /// </summary>
    /// <param name="topicId">The topic id.</param>
    /// <param name="otherUserId">The other user id.</param>
    /// <returns>The comparison.</returns>
    [HttpGet("Topics/{topicId}/Compare/{otherUserId}")]
    public async Task<ActionResult<CompareResponse>> Compare([FromRoute] Guid topicId, [FromRoute] Guid otherUserId)
    {
        var topic = await _store.GetTopicAsync(topicId).ConfigureAwait(false);
        if (topic is null)
        {
            return NotFound();
        }

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var mine = await _store.GetRankingAsync(topicId, userId).ConfigureAwait(false);
        var theirs = await _store.GetRankingAsync(topicId, otherUserId).ConfigureAwait(false);
        if (mine is null || theirs is null)
        {
            return NotFound("Both users need a ranking for this topic.");
        }

        var mineResolved = Resolve(mine);
        var theirsResolved = Resolve(theirs);
        var result = RankingComparer.Compare(mineResolved, theirsResolved, topic.Size);
        var movies = mineResolved.Entries.Concat(theirsResolved.Entries)
            .GroupBy(e => e.Key)
            .ToDictionary(g => g.Key, g => ToMovie(g.First()));

        return Ok(new CompareResponse(GetUserName(userId), GetUserName(otherUserId), result, movies));
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirst(UserIdClaim)?.Value, out userId) && userId != Guid.Empty;
    }

    private string GetUserName(Guid userId)
    {
        return _userManager.GetUserById(userId)?.Username ?? "Unknown user";
    }

    private static string? Validate(RankingEntry entry)
    {
        if (entry.ItemId != Guid.Empty)
        {
            return null;
        }

        if (string.IsNullOrEmpty(entry.TmdbId) || entry.TmdbId.Length > 12 || !entry.TmdbId.All(char.IsAsciiDigit))
        {
            return "Every entry needs a library item or a TMDb id.";
        }

        if (string.IsNullOrWhiteSpace(entry.Title) || entry.Title.Length > MaxMovieTitleLength)
        {
            return $"Movie title must be 1-{MaxMovieTitleLength} characters.";
        }

        if (entry.Year is < 1888 or > 2200)
        {
            return "Movie year is out of range.";
        }

        if (entry.Poster is not null && !entry.Poster.StartsWith(PosterPrefix, StringComparison.Ordinal))
        {
            return "Poster must be a TMDb image.";
        }

        return null;
    }

    private UserRanking Resolve(UserRanking ranking)
    {
        return new UserRanking
        {
            TopicId = ranking.TopicId,
            UserId = ranking.UserId,
            UpdatedAt = ranking.UpdatedAt,
            Entries = ranking.Entries.Select(Resolve).ToList()
        };
    }

    /// <summary>
    /// Refreshes a library entry from the library (title, year, TMDb id); other entries are returned as they are.
    /// </summary>
    private RankingEntry Resolve(RankingEntry entry)
    {
        if (entry.ItemId == Guid.Empty || _libraryManager.GetItemById(entry.ItemId) is not Movie movie)
        {
            return entry;
        }

        return new RankingEntry
        {
            Rank = entry.Rank,
            ItemId = entry.ItemId,
            TmdbId = movie.GetProviderId(MediaBrowser.Model.Entities.MetadataProvider.Tmdb) ?? entry.TmdbId,
            Title = movie.Name,
            Year = movie.ProductionYear ?? entry.Year,
            Poster = entry.Poster
        };
    }

    private MovieDto ToMovie(RankingEntry entry)
    {
        return new MovieDto(
            entry.Title ?? "Unknown movie",
            entry.Year,
            entry.ItemId == Guid.Empty ? null : entry.ItemId,
            entry.Poster);
    }

    private RankedEntryDto ToDto(RankingEntry stored)
    {
        var entry = Resolve(stored);
        return new RankedEntryDto(entry.Rank, entry.ItemId, entry.TmdbId, entry.Title ?? "Unknown movie", entry.Year, entry.Poster);
    }
}
