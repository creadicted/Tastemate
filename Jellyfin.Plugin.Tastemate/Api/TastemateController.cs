using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Jellyfin.Plugin.Tastemate.Models;
using Jellyfin.Plugin.Tastemate.Services;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
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

    private readonly TopStore _store;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TastemateController"/> class.
    /// </summary>
    /// <param name="store">Instance of the <see cref="TopStore"/> class.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    public TastemateController(TopStore store, ILibraryManager libraryManager, IUserManager userManager)
    {
        _store = store;
        _libraryManager = libraryManager;
        _userManager = userManager;
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

        if (entries.Select(e => e.ItemId).Distinct().Count() != entries.Count)
        {
            return BadRequest("A movie can only be ranked once.");
        }

        if (entries.Any(e => _libraryManager.GetItemById(e.ItemId) is not Movie))
        {
            return BadRequest("Every entry must be a movie in the library.");
        }

        var ranking = await _store.SetRankingAsync(topicId, userId, entries).ConfigureAwait(false);
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

        var result = RankingComparer.Compare(mine, theirs, topic.Size);
        var names = mine.Entries.Concat(theirs.Entries)
            .Select(e => e.ItemId)
            .Distinct()
            .ToDictionary(id => id, GetItemName);

        return Ok(new CompareResponse(GetUserName(userId), GetUserName(otherUserId), result, names));
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirst(UserIdClaim)?.Value, out userId) && userId != Guid.Empty;
    }

    private string GetUserName(Guid userId)
    {
        return _userManager.GetUserById(userId)?.Username ?? "Unknown user";
    }

    private string GetItemName(Guid itemId)
    {
        return _libraryManager.GetItemById(itemId)?.Name ?? "Unknown movie";
    }

    private RankedEntryDto ToDto(RankingEntry entry)
    {
        return new RankedEntryDto(entry.Rank, entry.ItemId, GetItemName(entry.ItemId));
    }
}
