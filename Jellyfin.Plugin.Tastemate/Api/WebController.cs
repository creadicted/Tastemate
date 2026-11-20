using System;
using System.IO;
using System.Net.Mime;
using Jellyfin.Plugin.Tastemate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Tastemate.Api;

/// <summary>
/// Serves the Tastemate page to every user, and the script that adds it to the user menu.
/// </summary>
[ApiController]
[Route("Tastemate")]
public class WebController : ControllerBase
{
    private const string ResourcePrefix = "Jellyfin.Plugin.Tastemate.";

    private readonly UserMenuService _userMenu;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebController"/> class.
    /// </summary>
    /// <param name="userMenu">Instance of the <see cref="UserMenuService"/> class.</param>
    public WebController(UserMenuService userMenu)
    {
        _userMenu = userMenu;
    }

    /// <summary>
    /// Gets the Tastemate page. It holds no data, the page calls the API with the user's own session.
    /// </summary>
    /// <returns>The page.</returns>
    [HttpGet("Page")]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Text.Html)]
    public ActionResult GetPage()
    {
        var html = ReadResource("Configuration.configPage.html");
        if (html is null)
        {
            return NotFound();
        }

        const string Head = "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><script src=\"standalone.js\"></script>";
        html = html.Replace("</head>", Head + "</head>", StringComparison.Ordinal);
        Response.Headers.CacheControl = "no-cache";
        return Content(html, "text/html; charset=utf-8");
    }

    /// <summary>
    /// Gets the script that lets the page run outside the web client.
    /// </summary>
    /// <returns>The script.</returns>
    [HttpGet("standalone.js")]
    [AllowAnonymous]
    public ActionResult GetStandaloneScript()
    {
        return Script("Web.standalone.js");
    }

    /// <summary>
    /// Gets the script that adds Tastemate to the user menu.
    /// </summary>
    /// <returns>The script.</returns>
    [HttpGet("inject.js")]
    [AllowAnonymous]
    public ActionResult GetMenuScript()
    {
        return Script("Web.inject.js");
    }

    /// <summary>
    /// Tells whether Tastemate is added to the user menu.
    /// </summary>
    /// <returns>Whether the File Transformation plugin is installed and the menu entry registered.</returns>
    [HttpGet("Menu")]
    [Authorize]
    public ActionResult<bool> GetMenuStatus()
    {
        return Ok(_userMenu.Registered);
    }

    private ActionResult Script(string name)
    {
        var script = ReadResource(name);
        if (script is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "no-cache";
        return Content(script, "application/javascript; charset=utf-8");
    }

    private static string? ReadResource(string name)
    {
        using var stream = typeof(WebController).Assembly.GetManifestResourceStream(ResourcePrefix + name);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
