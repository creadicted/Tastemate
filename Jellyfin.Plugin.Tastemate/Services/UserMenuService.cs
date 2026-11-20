using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Text.Json;
using MediaBrowser.Common.Plugins;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Tastemate.Services;

/// <summary>
/// Adds Tastemate to the web client's user menu through the File Transformation plugin, when that is installed.
/// </summary>
public class UserMenuService
{
    private static readonly Guid FileTransformationId = new("5e87cc92-571a-4d8d-8d98-d2d4147f9f90");
    private static readonly Guid TransformationId = new("3c4c8f1e-6a52-4f0b-9d6e-0b7a1d2f5c11");

    private readonly IPluginManager _pluginManager;
    private readonly ILogger<UserMenuService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserMenuService"/> class.
    /// </summary>
    /// <param name="pluginManager">Instance of the <see cref="IPluginManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
    public UserMenuService(IPluginManager pluginManager, ILogger<UserMenuService> logger)
    {
        _pluginManager = pluginManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether the menu entry was registered.
    /// </summary>
    public bool Registered { get; private set; }

    /// <summary>
    /// Registers the menu entry when the File Transformation plugin is installed.
    /// </summary>
    public void Register()
    {
        if (!_pluginManager.Plugins.Any(p => p.Id == FileTransformationId && p.Instance is not null))
        {
            _logger.LogInformation("File Transformation is not installed, Tastemate is not added to the user menu (it is still available at /Tastemate/Page)");
            return;
        }

        try
        {
            // The plugin cannot be referenced, so it is called by name.
            var assembly = AssemblyLoadContext.All
                .SelectMany(c => c.Assemblies)
                .FirstOrDefault(a => a.FullName?.Contains(".FileTransformation", StringComparison.Ordinal) ?? false);
            var method = assembly?.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface")?.GetMethod("RegisterTransformation");
            var payloadType = method?.GetParameters().FirstOrDefault()?.ParameterType;
            var parse = payloadType?.GetMethod("Parse", [typeof(string)]);
            if (method is null || parse is null)
            {
                _logger.LogWarning("File Transformation was found but its interface is not as expected, Tastemate is not added to the user menu");
                return;
            }

            var json = JsonSerializer.Serialize(new Dictionary<string, string?>
            {
                ["id"] = TransformationId.ToString(),
                ["fileNamePattern"] = "index.html",
                ["callbackAssembly"] = typeof(UserMenuService).Assembly.FullName,
                ["callbackClass"] = typeof(TransformationCallbacks).FullName,
                ["callbackMethod"] = nameof(TransformationCallbacks.IndexHtml)
            });
            method.Invoke(null, [parse.Invoke(null, [json])]);
            Registered = true;
            _logger.LogInformation("Tastemate added to the user menu");
        }
        catch (Exception ex) when (ex is System.Reflection.TargetInvocationException or InvalidOperationException or System.Reflection.AmbiguousMatchException)
        {
            _logger.LogWarning(ex, "Could not add Tastemate to the user menu");
        }
    }
}
