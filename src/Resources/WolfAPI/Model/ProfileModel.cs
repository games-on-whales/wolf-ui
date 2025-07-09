using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WolfUI;

namespace Resources.WolfAPI
{
    public class ProfilesResponse
    {
        [JsonInclude, JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonInclude, JsonPropertyName("profiles")]
        public List<Profile>? Profiles { get; set; }
    }
}

namespace WolfUI
{
    public partial class Profile
    {
        [JsonInclude, JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonInclude, JsonPropertyName("name")]
        public string? ProfileName { get; set; }
    
        [JsonInclude, JsonPropertyName("icon_png_path")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? IconPngPath { get; set; }

        [JsonInclude, JsonPropertyName("pin")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int>? Pin { get; set; }
        [JsonInclude, JsonPropertyName("apps")]
        public List<WolfUI.App>? Apps { get; set; }
    }   
}