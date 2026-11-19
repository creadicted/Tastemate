# Tastemate

A Jellyfin plugin for Top X movie lists and taste comparison between users.

Define a topic such as "Top 10 Horror Movies". Every user ranks movies from the library for that topic, then compares their list with another user's list to see how similar their taste is.

## Features

- **Topics**: any user can create a topic with a title and a size (number of entries). Only the creator or an administrator can delete it.
- **Rankings**: each user keeps one ranking per topic, built from movies in the library. A movie can only appear once per list and ranks must be unique.
- **Comparison**: compare your ranking with any other user's ranking for the same topic. The result lists shared movies with both ranks, movies only in one list, and a similarity score.
- **Dashboard page**: manage topics, edit your ranking and run comparisons from the plugin page ("Top Lists") in the Jellyfin dashboard.

### Similarity score

Every movie in both lists scores `1 - |rankA - rankB| / size`. The sum is divided by the topic size and shown as a percentage. Movies in only one list score 0. Identical lists give 100%.

## Requirements

- Jellyfin 10.11.x (target ABI `10.11.0.0`)
- [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet) to build

## Build

```shell
dotnet build Jellyfin.Plugin.Tastemate.slnx
```

Or publish a release build:

```shell
dotnet publish Jellyfin.Plugin.Tastemate/Jellyfin.Plugin.Tastemate.csproj -c Release
```

## Install

1. Build the plugin.
2. Copy `Jellyfin.Plugin.Tastemate.dll` into a `Tastemate` folder inside the Jellyfin plugins directory, for example `%LOCALAPPDATA%\jellyfin\plugins\Tastemate\` on Windows.
3. Restart Jellyfin.

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `MaxTopSize` | 25 | Maximum number of entries a topic may have. |

## API

All endpoints require an authenticated user and live under `/Tastemate`.

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/Tastemate/Topics` | List topics, newest first. |
| POST | `/Tastemate/Topics` | Create a topic (`Title`, `Size`). |
| DELETE | `/Tastemate/Topics/{topicId}` | Delete a topic (creator or administrator). |
| GET | `/Tastemate/Topics/{topicId}/Rankings` | List users who ranked the topic. |
| GET | `/Tastemate/Topics/{topicId}/Rankings/{userId}` | Get a user's ranking. |
| PUT | `/Tastemate/Topics/{topicId}/Ranking` | Replace the caller's ranking. |
| GET | `/Tastemate/Topics/{topicId}/Compare/{otherUserId}` | Compare the caller with another user. |

## Data

Topics and rankings are stored in `Jellyfin.Plugin.Tastemate.data.json` in the Jellyfin plugin configurations directory.

## License

GPLv3, see [LICENSE](LICENSE). The plugin links against Jellyfin's GPLv3 packages.
