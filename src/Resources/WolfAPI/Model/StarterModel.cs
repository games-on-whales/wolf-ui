using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Resources.WolfAPI;
public class Starter
{
    [JsonInclude, JsonPropertyName("stop_stream_when_over")]
    public bool StopStreamWhenOver { get; set; }
    [JsonInclude, JsonPropertyName("session_id")]
    public string? SessionId { get; set; }
    [JsonInclude, JsonPropertyName("runner")]
    public Runner? Runner { get; set; }
}