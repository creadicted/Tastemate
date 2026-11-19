# Tastemate

A Jellyfin plugin for Top X movie lists and taste comparison between users.

Define a topic such as "Top 10 Horror Movies". Every user ranks movies from the library for that topic, then compares their list with another user's list to see how similar their taste is.

## Features

- **Topics**: any user can create a topic with a title and a size (number of entries). Only the creator or an administrator can delete it.
- **Rankings**: each user keeps one ranking per topic, built from movies in the library. A movie can only appear once per list and ranks must be unique.
- **Comparison**: compare your ranking with any other user's ranking for the same topic. The result lists shared movies with both ranks, movies only in one list, and a similarity score.
- **Dashboard page**: manage topics, edit your ranking and run comparisons from the "Tastemate" page in the Jellyfin dashboard sidebar.

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

### From the plugin repository (recommended)

1. In Jellyfin open Dashboard > Plugins > Repositories and click Add.
2. Enter the name `Tastemate` and the URL `https://github.com/creadicted/Tastemate/releases/latest/download/manifest.json`.
3. Go to the Catalog, open Tastemate and click Install.
4. Restart Jellyfin.
5. Open Tastemate in the dashboard sidebar (under Plugins) to create topics and rank movies.

Updates appear in the Catalog and under My Plugins. The scheduled task "Update Plugins" can install them automatically. A restart is needed after each update.

### Manual

1. Download `tastemate_<version>.zip` from the [latest release](https://github.com/creadicted/Tastemate/releases/latest) and unzip it, or build the plugin yourself.
2. Copy `Jellyfin.Plugin.Tastemate.dll` into a `Tastemate` folder inside the Jellyfin plugins directory:
   - Windows: `%LOCALAPPDATA%\jellyfin\plugins\Tastemate\`
   - Linux: `/var/lib/jellyfin/plugins/Tastemate/`
   - Docker: `<config volume>/plugins/Tastemate/`
3. Restart Jellyfin.

## Local development

`test/serve.sh` runs Jellyfin in a container with your current build of the plugin. It is a bash script, so run it from WSL (Docker) or Git Bash, not from PowerShell or cmd (from those: `wsl bash test/serve.sh`). It builds with `dotnet`, or with the Windows `dotnet.exe` through WSL, or as a last resort inside the .NET SDK container (which fails behind proxies that inspect TLS).

```shell
test/serve.sh                              # build, start on http://localhost:8096
MEDIA_DIR=~/movies test/serve.sh           # also mount movies at /media (read-only)
test/serve.sh                              # after code changes: rebuild, copy DLL, restart
test/serve.sh --stop                       # stop, keep Jellyfin data
test/serve.sh --reset                      # stop and delete all data
```

On first start complete the Jellyfin setup wizard, add a Movies library pointing at `/media`, then open Dashboard > Plugins > Tastemate. Config and data live in the `tastemate-dev-config` volume and survive restarts. `PORT` and `TAG` env vars change the host port and Jellyfin version.

## Testing

`test/e2e.sh` runs the plugin end to end against a fresh Jellyfin container (Docker or Podman). It builds the plugin, serves it through a local repository manifest, installs it via the Jellyfin API, restarts the server and exercises the topic endpoints.

```shell
test/e2e.sh          # Jellyfin 10.11.5
test/e2e.sh 10.11.6  # another image tag
```

Needs `docker` or `podman`, `dotnet`, `curl` and `python`.

## Continuous integration and releases

- **Test** (`.github/workflows/test.yaml`): on every push to `main` and every pull request, builds the plugin and runs `test/e2e.sh` against a Jellyfin container.
- **Release** (`.github/workflows/release.yaml`): pushing a tag that starts with `v` builds the plugin, zips the DLL and creates a GitHub release with `tastemate_<version>.zip` and `manifest.json`. The manifest keeps all earlier versions, so the repository URL under Install always serves the latest.

To release:

```shell
git tag v0.3.0
git push origin v0.3.0
```

The tag becomes the plugin version (`v0.3.0` is published as `0.3.0.0`). Versions must increase, and a tag must not be reused. When Jellyfin moves to a new major version, update `targetAbi` in `build.yaml` and `release.yaml` and the `Jellyfin.Controller` and `Jellyfin.Model` package versions in the csproj.

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `MaxTopSize` | 25 | Maximum number of entries a topic may have. |

There is no UI for this yet. To change it, edit `Jellyfin.Plugin.Tastemate.xml` in the Jellyfin plugin configurations directory (`<config>/plugins/configurations/`) while the server is stopped.

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
