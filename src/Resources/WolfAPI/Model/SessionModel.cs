using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Resources.WolfAPI;

public class Session
{
    [JsonInclude, JsonPropertyName("app_id")]
    public string? AppId {get;set;}
    [JsonInclude, JsonPropertyName("client_id")]
    public string? ClientId {get;set;}
    [JsonInclude, JsonPropertyName("client_ip")]
    public string? ClientIp {get;set;}
    [JsonInclude, JsonPropertyName("video_width")]
    public int VideoWidth {get;set;}
    [JsonInclude, JsonPropertyName("video_height")]
    public int VideoHeight {get;set;}
    [JsonInclude, JsonPropertyName("video_refresh_rate")]
    public int VideoRefreshRate {get;set;}
    [JsonInclude, JsonPropertyName("audio_channel_count")]
    public int AudioChannelCount {get;set;}
    [JsonInclude, JsonPropertyName("client_settings")]
    public ClientSettings? ClientSettings {get;set;}
}

public class SessionsResponse
{
    [JsonInclude, JsonPropertyName("success")]
    public bool Success {get;set;}
    [JsonInclude, JsonPropertyName("sessions")]
    public List<Session>? Sessions {get;set;}
}