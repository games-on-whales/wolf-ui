using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Resources.WolfAPI;

public class ClientSettings
{
    [JsonInclude, JsonPropertyName("controllers_override")]
    public List<string>? ControllersOverride {get;set;}
    [JsonInclude, JsonPropertyName("h_scroll_acceleration")]
    public float HScrollAcceleration {get;set;}
    [JsonInclude, JsonPropertyName("mouse_acceleration")]
    public float MouseAcceleration {get;set;}
    [JsonInclude, JsonPropertyName("run_gid")]
    public int RunGid {get;set;}
    [JsonInclude, JsonPropertyName("run_uid")]
    public int RunUid {get;set;}
    [JsonInclude, JsonPropertyName("v_scroll_acceleration")]
    public float VScrollAcceleration {get;set;}
}