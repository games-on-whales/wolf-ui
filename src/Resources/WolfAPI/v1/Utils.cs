using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Godot;
using WolfUI;

namespace Resources.WolfAPI;

public partial class WolfApi
{
    private static string? _iconCacheDir;
    private static string IconCacheDir => _iconCacheDir ??= ResolveIconCacheDir();

    // Wolf mounts the app state folder on /home/retro, so cache there to keep icons
    // across sessions. When that isn't writable (e.g. rootless, where nobody chowns it
    // for us) fall back to $HOME, then Godot's user dir. An empty string disables the cache.
    private static string ResolveIconCacheDir()
    {
        var user = System.Environment.GetEnvironmentVariable("USER") ?? "retro";
        user = user == "root" ? "retro" : user;

        string?[] homes = [$"/home/{user}", System.Environment.GetEnvironmentVariable("HOME"), OS.GetUserDataDir()];
        foreach (var home in homes)
        {
            if (string.IsNullOrEmpty(home))
                continue;

            var dir = Path.Combine(home, ".wolf-ui/tmp/icons");
            try
            {
                Directory.CreateDirectory(dir);
                return dir;
            }
            catch (Exception e) when (e is UnauthorizedAccessException or IOException)
            {
                Logger.LogDebug("Icon cache {0} not usable: {1}", dir, e.Message);
            }
        }

        Logger.LogWarning("No writable icon cache directory, icons won't be cached");
        return "";
    }

    public static async Task<Texture2D?> GetIcon(App app, double hCacheDuration = 1.0, int retry = 0)
    {
        if (app.IconPngPath is not null && app.IconPngPath != "") // image set, get image from wolf
            return await GetIcon(app.IconPngPath, hCacheDuration, retry);

        if (app.Runner?.Image is null || !app.Runner.Image.Contains("ghcr.io/games-on-whales/"))
            return null;

        var name = app.Runner.Image.TrimPrefix("ghcr.io/games-on-whales/");//.TrimSuffix(":edge");
        var idx = name.LastIndexOf(':');
        if (idx >= 0)
            name = name[..idx];

        return await GetIcon($"https://games-on-whales.github.io/wildlife/apps/{name}/assets/icon.png", hCacheDuration, retry);
    }
    public static async Task<Texture2D?> GetIcon(string iconPath, double hCacheDuration = 1.0, int retry = 0)
    {
        if (retry >= 5)
        {
            Logger.LogError("Failed Loading {0} 5 times, skipping", iconPath);
            return null;
        }
        var filepath = IconCacheDir == "" ? null : $"{IconCacheDir}/{iconPath}.png".Replace("\\", "/").Replace("//", "/");

        if (filepath is not null && File.Exists(filepath))
        {
            if (File.GetCreationTime(filepath).AddHours(hCacheDuration).CompareTo(DateTime.Now) >= 0)
            {
                var image = Image.LoadFromFile(filepath);
                return ImageTexture.CreateFromImage(image);
            }

            try
            {
                File.Delete(filepath);
            }
            catch (Exception e) when (e is UnauthorizedAccessException or IOException)
            {
                Logger.LogWarning("Could not remove stale icon {0}: {1}", filepath, e.Message);
            }
        }

        Logger.LogInformation("Requesting icon: {0}", iconPath);

        HttpResponseMessage message;
        try
        {
            message = await HttpClient.GetAsync($"{Api}/utils/get-icon?icon_path={iconPath}");
        }
        catch (HttpRequestException e)
        {
            if(e.InnerException is not null)
                Logger.LogWarning("Icon {0} could not be accessed: {1} - {2} Retrying", iconPath, e.Message, e.InnerException.Message);
            else
                Logger.LogWarning("Icon {0} could not be accessed: {1} Retrying", iconPath, e.Message);
            return await GetIcon(iconPath, hCacheDuration, retry + 1);
        }

        if (message.StatusCode == HttpStatusCode.OK)
        {
            Image image = new();
            var error = await image.LoadImageFromHttpResonseMessage(message);
            if (error != Error.Ok)
            {
                if (error == Error.FileUnrecognized)
                {
                    return null;
                }

                Logger.LogError("Icon {0} could not be decoded properly, Retrying", iconPath);
                return await GetIcon(iconPath, hCacheDuration, retry + 1);
            }

            if (filepath is not null && Path.GetDirectoryName(filepath) is { } directoryPath)
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                    var saveError = image.SavePng(filepath);
                    if (saveError != Error.Ok)
                        Logger.LogWarning("Could not cache icon {0}: {1}", filepath, saveError);
                }
                catch (Exception e) when (e is UnauthorizedAccessException or IOException)
                {
                    Logger.LogWarning("Could not cache icon {0}: {1}", filepath, e.Message);
                }
            }
            var texture = ImageTexture.CreateFromImage(image);
            return texture;
        }
        Logger.LogError("Could not access image: {0}: {1}", iconPath, message.StatusCode);
        return null;
    }
}
