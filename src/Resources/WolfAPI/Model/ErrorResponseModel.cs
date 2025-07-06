using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Resources.WolfAPI;

public class ErrorResponse
{
    [JsonInclude, JsonPropertyName("success")]
    public bool Success { get; init; }
    [JsonInclude, JsonPropertyName("error")]
    public string? Error { get; init; }
}