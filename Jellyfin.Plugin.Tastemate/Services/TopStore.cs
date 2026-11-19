using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Tastemate.Models;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.Tastemate.Services;

/// <summary>
/// Persists topics and rankings in a JSON file.
/// </summary>
public sealed class TopStore : IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private TastemateData? _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="TopStore"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    public TopStore(IApplicationPaths applicationPaths)
    {
        _filePath = Path.Combine(applicationPaths.PluginConfigurationsPath, "Jellyfin.Plugin.Tastemate.data.json");
    }

    /// <summary>
    /// Gets all topics, newest first.
    /// </summary>
    /// <returns>The topics.</returns>
    public async Task<IReadOnlyList<Topic>> GetTopicsAsync()
    {
        return await ReadAsync(d => (IReadOnlyList<Topic>)d.Topics.OrderByDescending(t => t.CreatedAt).ToList()).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a topic.
    /// </summary>
    /// <param name="topicId">The topic id.</param>
    /// <returns>The topic or <c>null</c>.</returns>
    public async Task<Topic?> GetTopicAsync(Guid topicId)
    {
        return await ReadAsync(d => d.Topics.FirstOrDefault(t => t.Id == topicId)).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a topic.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="size">The number of entries.</param>
    /// <param name="userId">The creating user.</param>
    /// <returns>The new topic.</returns>
    public async Task<Topic> CreateTopicAsync(string title, int size, Guid userId)
    {
        var topic = new Topic
        {
            Id = Guid.NewGuid(),
            Title = title,
            Size = size,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await WriteAsync(d => d.Topics.Add(topic)).ConfigureAwait(false);
        return topic;
    }

    /// <summary>
    /// Deletes a topic and all its rankings.
    /// </summary>
    /// <param name="topicId">The topic id.</param>
    /// <returns><c>true</c> if the topic existed.</returns>
    public async Task<bool> DeleteTopicAsync(Guid topicId)
    {
        var removed = false;
        await WriteAsync(d =>
        {
            removed = d.Topics.RemoveAll(t => t.Id == topicId) > 0;
            d.Rankings.RemoveAll(r => r.TopicId == topicId);
        }).ConfigureAwait(false);
        return removed;
    }

    /// <summary>
    /// Gets all rankings of a topic.
    /// </summary>
    /// <param name="topicId">The topic id.</param>
    /// <returns>The rankings.</returns>
    public async Task<IReadOnlyList<UserRanking>> GetRankingsAsync(Guid topicId)
    {
        return await ReadAsync(d => (IReadOnlyList<UserRanking>)d.Rankings.Where(r => r.TopicId == topicId).ToList()).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a user's ranking for a topic.
    /// </summary>
    /// <param name="topicId">The topic id.</param>
    /// <param name="userId">The user id.</param>
    /// <returns>The ranking or <c>null</c>.</returns>
    public async Task<UserRanking?> GetRankingAsync(Guid topicId, Guid userId)
    {
        return await ReadAsync(d => d.Rankings.FirstOrDefault(r => r.TopicId == topicId && r.UserId == userId)).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces a user's ranking for a topic.
    /// </summary>
    /// <param name="topicId">The topic id.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="entries">The new entries.</param>
    /// <returns>The stored ranking.</returns>
    public async Task<UserRanking> SetRankingAsync(Guid topicId, Guid userId, IReadOnlyList<RankingEntry> entries)
    {
        var ranking = new UserRanking
        {
            TopicId = topicId,
            UserId = userId,
            Entries = entries.OrderBy(e => e.Rank).ToList(),
            UpdatedAt = DateTime.UtcNow
        };

        await WriteAsync(d =>
        {
            d.Rankings.RemoveAll(r => r.TopicId == topicId && r.UserId == userId);
            d.Rankings.Add(ranking);
        }).ConfigureAwait(false);
        return ranking;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _lock.Dispose();
    }

    private async Task<T> ReadAsync<T>(Func<TastemateData, T> read)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            return read(await LoadAsync().ConfigureAwait(false));
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task WriteAsync(Action<TastemateData> write)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var data = await LoadAsync().ConfigureAwait(false);
            write(data);

            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var tempPath = _filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(data, _jsonOptions)).ConfigureAwait(false);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<TastemateData> LoadAsync()
    {
        if (_data is not null)
        {
            return _data;
        }

        if (File.Exists(_filePath))
        {
            var json = await File.ReadAllTextAsync(_filePath).ConfigureAwait(false);
            _data = JsonSerializer.Deserialize<TastemateData>(json);
        }

        return _data ??= new TastemateData();
    }
}
