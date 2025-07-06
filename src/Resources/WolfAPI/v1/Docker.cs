using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Godot;

namespace Resources.WolfAPI;

public partial class WolfApi
{
    //TODO Create Class for the Inspect return. 
    public static async Task InspectImage(string imageName)
    {
        var response = await _httpClient.GetAsync($"{Api}/docker/images/inspect?image_name={imageName}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }
        if (response.StatusCode == HttpStatusCode.OK)
        {

        }
        return;
    }

    public static async Task<bool> IsImageOnDisk(string imageName, int retryCount = 0)
    {
        var cacheKey = $"{Api}/docker/images/inspect?image_name={imageName}";

        if (_cache.Contains(cacheKey))
        {
            return _cache.Get(cacheKey) as string != "";
        }

        if (retryCount >= 5)
        {
            Logger.LogError("Api call failed 5 times: /docker/images/inspect?image_name={0}, {1}... ABORT", imageName);
            return false;
        }

        try
        {
            var response = await _httpClient.GetAsync(cacheKey);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _cache.Add(cacheKey, "", WolfAPICachePolicy);
                return false;
            }
            var str = await response.Content.ReadAsStringAsync();
            _cache.Add(cacheKey, str, WolfAPICachePolicy);
            return true;
        }
        catch (HttpRequestException e)
        {
            Logger.LogWarning("Api call failed: /docker/images/inspect?image_name={0}, {1}... Retrying", imageName, e.Message);
            return await IsImageOnDisk(imageName, retryCount + 1);
        }
    }

    private sealed record PullImageResponse
    {
        [JsonInclude, JsonPropertyName("success")]
        public bool? Success { get; init; }
        [JsonInclude, JsonPropertyName("layer_id")]
        public string? LayerId { get; init; }
        [JsonInclude, JsonPropertyName("current_progress")]
        public long CurrentProgress { get; init; }
        [JsonInclude, JsonPropertyName("total")]
        public long Total { get; init; }
    }

    public static void PullImage(string imageName)
    {
        _ = Task.Run(async () =>
        {
            var json = $$"""
             {
                 "image_name": "{{imageName}}"
             }
             """;

            EmitSignalDeferredImagePullProgress(imageName, 0.0);

            Logger.LogInformation("Pulling image: {0}", imageName);

            var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"{Api}/docker/images/pull")
            {
                Content = new StringContent(json)
            };

            var response = await _httpClient.SendAsync(reqMsg, HttpCompletionOption.ResponseHeadersRead);
            Logger.LogInformation("Pull request: {0}", response.StatusCode);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.LogError("Cant Pull Image: {0}, {1}", imageName, response.StatusCode);
            }

            var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            Dictionary<string, PullImageResponse> layerSizes = [];
            var isUnpacking = false;
            long lastCurrent = 0;
            var hasDownloaded = false;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line is null)
                    continue;

                var parsed = JsonSerializer.Deserialize<PullImageResponse>(line, JsonOptions);
                if (parsed is null)
                    continue;


                if (parsed.Success is not null && parsed.Success == true)
                {
                    var cacheKey = $"{Api}/docker/images/inspect?image_name={imageName}";
                    if (_cache.Contains(cacheKey))
                        _cache.Remove(cacheKey);
                    _cache.Add(cacheKey, "exists", WolfAPICachePolicy);

                    if (hasDownloaded)
                        EmitSignalDeferredImageUpdated(imageName);
                    else
                        EmitSignalDeferredImageAlreadyUptoDate(imageName);

                    Logger.LogInformation("Image: {0} {1}", imageName, hasDownloaded ? "was Updated" : "is already up to Date");
                    return;
                }

                hasDownloaded = true;

                if (parsed.LayerId is null)
                    continue;

                layerSizes[parsed.LayerId] = new PullImageResponse
                {
                    LayerId = parsed.LayerId,
                    CurrentProgress = parsed.CurrentProgress,
                    Total = parsed.Total
                };

                long current = 0;
                long total = 0;

                foreach (var kv in layerSizes)
                {
                    current += kv.Value.CurrentProgress;
                    total += kv.Value.Total;
                }

                var sizeTotal = total;
                total *= 2;

                if (total <= 1000) continue;
                
                if (lastCurrent > 0 && lastCurrent > current + 0.3 * lastCurrent)
                    isUnpacking = true;
                lastCurrent = current;
                var percentProgress = 100.0 * (current + (isUnpacking ? sizeTotal : 0)) / total;
                EmitSignalDeferredImagePullProgress(imageName, percentProgress);
            }

            return;

            void EmitSignalDeferredImagePullProgress(string argImageName, double progress) => Singleton.CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.ImagePullProgress, argImageName, progress);
            void EmitSignalDeferredImageAlreadyUptoDate(string argImageName) => Singleton.CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.ImageAlreadyUptoDate, argImageName);
            void EmitSignalDeferredImageUpdated(string argImageName) => Singleton.CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.ImageUpdated, argImageName);
        });
    }
}