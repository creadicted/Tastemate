using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Tastemate.Models;

namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// Request to create a topic.
/// </summary>
/// <param name="Title">The title.</param>
/// <param name="Size">The number of entries.</param>
public record CreateTopicRequest(string Title, int Size);
