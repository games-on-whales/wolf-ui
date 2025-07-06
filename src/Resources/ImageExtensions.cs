using System.Net.Http;
using System.Threading.Tasks;
using Godot;
using WolfUI;

namespace Resources.WolfAPI;

public static class ImageExtensions
{
    private static readonly ILogger<Image> Logger = WolfUI.Main.GetLogger<Image>();

    public static async Task<Error> LoadImageFromHttpResonseMessage(this Image image, HttpResponseMessage message)
    {
        if (message.Content.Headers.ContentType?.MediaType is null)
        {
            return Error.DoesNotExist;
        }

        var mediaType = message.Content.Headers.ContentType.MediaType;
        if (mediaType is null)
            return default;

        var error = mediaType switch
        {
            "image/png" => image.LoadPngFromBuffer(await message.Content.ReadAsByteArrayAsync()),
            _ => Error.FileUnrecognized
        };

        if (error == Error.FileUnrecognized)
        {
            Logger.LogError("No Load function for \"{0}\" currently implemented", mediaType);
        }

        return error;
    }

}
