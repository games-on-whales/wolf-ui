using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Resources.WolfAPI;

public class VideoSettings
{
    [JsonInclude, JsonPropertyName("width")]
    public int Width { get; set; }
    [JsonInclude, JsonPropertyName("height")]
    public int Height { get; set; }
    [JsonInclude, JsonPropertyName("refresh_rate")]
    public int RefreshRate { get; set; }
    [JsonInclude, JsonPropertyName("wayland_render_node")]
    public string? WaylandRenderNode { get; set; }
    [JsonInclude, JsonPropertyName("runner_render_node")]
    public string? RunnerRenderNode { get; set; }
    [JsonInclude, JsonPropertyName("video_producer_buffer_caps")]
    public string? VideoProducerBufferCaps { get; set; }
}