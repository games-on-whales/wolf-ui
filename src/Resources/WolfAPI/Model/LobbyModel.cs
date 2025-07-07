using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Resources.WolfAPI;

public partial class Lobby
{
    [JsonInclude, JsonPropertyName("profile_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProfileId { get; set; }
    [JsonInclude, JsonPropertyName("started_by_profile_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StartedByProfileId { get; set; }
    [JsonInclude, JsonPropertyName("id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }
    [JsonInclude, JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonInclude, JsonPropertyName("icon_png_path")]
    public string? IconPngPath { get; set; }
    [JsonInclude, JsonPropertyName("pin_required")]
    public bool? PinRequired { get; set; }
    public bool IsPinLocked => Pin is not null || (PinRequired is not null && PinRequired == true);
    [JsonInclude, JsonPropertyName("pin")]
    public List<int>? Pin { get; set; }
    [JsonInclude, JsonPropertyName("multi_user")]
    public bool MultiUser { get; set; }
    [JsonInclude, JsonPropertyName("stop_when_everyone_leaves")]
    public bool StopWhenEveryoneLeaves { get; set; }
    [JsonInclude, JsonPropertyName("runner_state_folder")]
    public string? RunnerStateFolder { get; set; }
    [JsonInclude, JsonPropertyName("runner")]
    public Runner? Runner { get; set; }
    [JsonInclude, JsonPropertyName("client_settings"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ClientSettings? ClientSettings { get; set; }
    [JsonInclude, JsonPropertyName("video_settings"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VideoSettings? VideoSettings { get; set; }
    [JsonInclude, JsonPropertyName("audio_settings"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AudioSettings? AudioSettings { get; set; }
    [JsonInclude, JsonPropertyName("connected_sessions")]
    public List<string>? ConnectedSessions { get; set; }
}

public class LobbyJoin
{
    [JsonInclude, JsonPropertyName("lobby_id")]
    public string? LobbyId { get; set; }
    [JsonInclude, JsonPropertyName("moonlight_session_id")]
    public string? MoonlightSessionId { get; set; }
    [JsonInclude, JsonPropertyName("pin")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<int>? Pin { get; set; }
}

public class LobbiesResponse
{
    [JsonInclude, JsonPropertyName("success")]
    public bool Success {get;set;}
    [JsonInclude, JsonPropertyName("lobbies")]
    public List<Lobby>? Lobbies {get;set;}
}

public class LobbyCreatedResponse
{
    [JsonInclude, JsonPropertyName("success")]
    public bool Success {get;set;}
    [JsonInclude, JsonPropertyName("lobby_id")]
    public string? LobbyId {get;set;}
}