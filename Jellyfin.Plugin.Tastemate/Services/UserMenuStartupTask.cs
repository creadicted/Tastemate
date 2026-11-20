using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.Tastemate.Services;

/// <summary>
/// Registers the user menu entry once the server has started.
/// </summary>
public class UserMenuStartupTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly UserMenuService _userMenu;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserMenuStartupTask"/> class.
    /// </summary>
    /// <param name="userMenu">Instance of the <see cref="UserMenuService"/> class.</param>
    public UserMenuStartupTask(UserMenuService userMenu)
    {
        _userMenu = userMenu;
    }

    /// <inheritdoc />
    public string Name => "Tastemate user menu";

    /// <inheritdoc />
    public string Key => "Jellyfin.Plugin.Tastemate.UserMenu";

    /// <inheritdoc />
    public string Description => "Adds Tastemate to the user menu when the File Transformation plugin is installed.";

    /// <inheritdoc />
    public string Category => "Startup Services";

    /// <inheritdoc />
    public bool IsHidden => true;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => false;

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _userMenu.Register();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo { Type = TaskTriggerInfoType.StartupTrigger };
    }
}
