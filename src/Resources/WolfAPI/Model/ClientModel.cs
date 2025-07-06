using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Resources.WolfAPI;

public class ClientsRequest
{
    [JsonInclude, JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonInclude, JsonPropertyName("clients")]
    public List<Client>? Clients { get; set; }
}

public class Client
{
    [JsonInclude, JsonPropertyName("client_id")]
    public string? ClientId { get; set; }
    [JsonInclude, JsonPropertyName("app_state_folder")]
    public string? AppStateFolder { get; set; }
    [JsonInclude, JsonPropertyName("client_cert")]
    public string? ClientCert { get; set; }
    [JsonInclude, JsonPropertyName("settings")]
    public ClientSettings? Settings { get; set; }
}