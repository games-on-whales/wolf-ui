
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Resources.WolfAPI;

public class Runner
{
    [JsonInclude, JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonInclude, JsonPropertyName("base_create_json"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BaseCreateJson { get; set; }

    [JsonInclude, JsonPropertyName("devices"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Devices { get; set; }

    [JsonInclude, JsonPropertyName("env"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Env { get; set; }

    [JsonInclude, JsonPropertyName("image"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Image { get; set; }

    [JsonInclude, JsonPropertyName("mounts"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Mounts { get; set; }

    [JsonInclude, JsonPropertyName("name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonInclude, JsonPropertyName("ports"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Ports { get; set; }

    [JsonInclude, JsonPropertyName("parent_session_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParentSessionId { get; set; }

    [JsonInclude, JsonPropertyName("run_cmd"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RunCmd { get; set; }
}