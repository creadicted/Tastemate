using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;

namespace Jellyfin.Plugin.Tastemate.Services;

/// <summary>
/// Talks to the Seerr REST API.
/// </summary>
public class SeerrClient
{
    private const int PageSize = 100;
    private const int MaxPages = 50;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeerrClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    public SeerrClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Gets a value indicating whether an address and API key are configured.
    /// </summary>
    public bool IsConfigured
    {
        get
        {
            var config = Plugin.Instance?.Configuration;
            return config is not null
                && !string.IsNullOrWhiteSpace(config.SeerrAddress)
                && !string.IsNullOrWhiteSpace(config.SeerrApiKey);
        }
    }

    /// <summary>
    /// Checks that an address and API key work.
    /// </summary>
    /// <param name="address">The Seerr address.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    public async Task<SeerrResult> TestAsync(string address, string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await SendAsync(HttpMethod.Get, address, apiKey, "auth/me", null, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return new SeerrResult(true, null);
            }

            return response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? new SeerrResult(false, "Seerr rejected the API key.")
                : new SeerrResult(false, $"Seerr answered with HTTP {(int)response.StatusCode}. Check the address.");
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new SeerrResult(false, "Could not reach Seerr. Check the address.");
        }
    }

    /// <summary>
    /// Gets the state of a movie in Seerr.
    /// </summary>
    /// <param name="tmdbId">The TMDb id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The state, or <see cref="SeerrMovieState.None"/> when unknown.</returns>
    public async Task<SeerrMovieState> GetStateAsync(int tmdbId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await SendConfiguredAsync(HttpMethod.Get, string.Create(CultureInfo.InvariantCulture, $"movie/{tmdbId}"), null, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return SeerrMovieState.None;
            }

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);
            if (document.RootElement.TryGetProperty("mediaInfo", out var info)
                && info.ValueKind == JsonValueKind.Object
                && info.TryGetProperty("status", out var status)
                && status.TryGetInt32(out var value))
            {
                return value switch
                {
                    2 => SeerrMovieState.Pending,
                    3 => SeerrMovieState.Processing,
                    4 => SeerrMovieState.PartiallyAvailable,
                    5 => SeerrMovieState.Available,
                    _ => SeerrMovieState.None
                };
            }

            return SeerrMovieState.None;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return SeerrMovieState.None;
        }
    }

    /// <summary>
    /// Finds the Seerr user that belongs to a Jellyfin user.
    /// </summary>
    /// <param name="jellyfinUserId">The Jellyfin user id.</param>
    /// <param name="username">The Jellyfin user name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Seerr user id, or null when there is no match.</returns>
    public async Task<int?> FindUserAsync(Guid jellyfinUserId, string? username, CancellationToken cancellationToken)
    {
        var wantedId = jellyfinUserId.ToString("N", CultureInfo.InvariantCulture);
        int? byName = null;
        try
        {
            for (var page = 0; page < MaxPages; page++)
            {
                var path = string.Create(CultureInfo.InvariantCulture, $"user?take={PageSize}&skip={page * PageSize}");
                using var response = await SendConfiguredAsync(HttpMethod.Get, path, null, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);
                if (!document.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                var count = 0;
                foreach (var user in results.EnumerateArray())
                {
                    count++;
                    if (!user.TryGetProperty("id", out var idElement) || !idElement.TryGetInt32(out var id))
                    {
                        continue;
                    }

                    var linkedId = ReadString(user, "jellyfinUserId")?.Replace("-", string.Empty, StringComparison.Ordinal);
                    if (string.Equals(linkedId, wantedId, StringComparison.OrdinalIgnoreCase))
                    {
                        return id;
                    }

                    if (byName is null
                        && !string.IsNullOrEmpty(username)
                        && string.Equals(ReadString(user, "jellyfinUsername"), username, StringComparison.OrdinalIgnoreCase))
                    {
                        byName = id;
                    }
                }

                if (count < PageSize)
                {
                    break;
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        return byName;
    }

    /// <summary>
    /// Requests a movie on behalf of a Seerr user.
    /// </summary>
    /// <param name="tmdbId">The TMDb id.</param>
    /// <param name="seerrUserId">The Seerr user id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    public async Task<SeerrResult> RequestMovieAsync(int tmdbId, int seerrUserId, CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object>
        {
            ["mediaType"] = "movie",
            ["mediaId"] = tmdbId,
            ["userId"] = seerrUserId
        };

        try
        {
            using var content = JsonContent.Create(body);
            using var response = await SendConfiguredAsync(HttpMethod.Post, "request", content, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return new SeerrResult(true, null);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.Conflict => new SeerrResult(true, null),
                HttpStatusCode.Forbidden => new SeerrResult(false, "Seerr refused the request. You may be over your request quota or lack the permission."),
                _ => new SeerrResult(false, $"Seerr answered with HTTP {(int)response.StatusCode}.")
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new SeerrResult(false, "Could not reach Seerr.");
        }
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private Task<HttpResponseMessage> SendConfiguredAsync(HttpMethod method, string path, HttpContent? content, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        return SendAsync(method, config?.SeerrAddress ?? string.Empty, config?.SeerrApiKey ?? string.Empty, path, content, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string address, string apiKey, string path, HttpContent? content, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, new Uri(address.TrimEnd('/') + "/api/v1/" + path))
        {
            Content = content
        };
        request.Headers.Add("X-Api-Key", apiKey);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        return await _httpClientFactory.CreateClient(NamedClient.Default).SendAsync(request, timeout.Token).ConfigureAwait(false);
    }
}
