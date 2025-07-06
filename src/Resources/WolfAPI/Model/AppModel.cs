using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Resources.WolfAPI
{
    public class AppsRequest
    {
        [JsonInclude, JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonInclude, JsonPropertyName("apps")]
        public List<WolfUI.App>? Apps { get; set; }
    }
}


namespace WolfUI
{
    public partial class App
    {
        [JsonInclude, JsonPropertyName("icon_png_path")]
        public string? IconPngPath {get;set;}
        [JsonInclude, JsonPropertyName("start_virtual_compositor")]
        public bool StartVirtualCompositor {get;set;}
        [JsonInclude, JsonPropertyName("title")]
        public string? Title {get;set;}
        [JsonInclude, JsonPropertyName("id")]
        public string? Id {get;set;}
        [JsonInclude, JsonPropertyName("runner")]
        public Resources.WolfAPI.Runner? Runner {get;set;}
        [JsonInclude, JsonPropertyName("render_node")]
        public string? RenderNode {get;set;}
    }
}
