using System.Text.Json.Serialization;

namespace Resources.WolfAPI;

public class AudioSettings
{
    [JsonInclude, JsonPropertyName("channel_count")]
    public int ChannelCount {get;set;}
}