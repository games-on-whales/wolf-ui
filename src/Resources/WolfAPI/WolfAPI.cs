using Godot;
using System;
using WolfUI;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using WolfUI.Misc;

namespace Resources.WolfAPI;

[GlobalClass]
public partial class WolfApi : Resource
{
    private static string ApiUrl { get; set; } = "http://localhost";
    private static string ApiPath { get; } = "/api/v1";
    private static string Api => $"{ApiUrl}{ApiPath}";

    private static readonly JsonSerializerOptions JsonOptions = new ()
    {
        TypeInfoResolver = new OptInJsonTypeInfoResolver()
    };
    public event EventHandler<Lobby>? LobbyCreatedEvent;
    public event EventHandler<string>? LobbyStoppedEvent;
    
    private static readonly System.Net.Http.HttpClient _httpClient = new(new SocketsHttpHandler
    {
        ConnectCallback = async (context, token) =>
        {
            var endpointPath = System.Environment.GetEnvironmentVariable("WOLF_SOCKET_PATH") ?? "/etc/wolf/cfg/wolf.sock";
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            var endpoint = new UnixDomainSocketEndPoint(endpointPath);
            await socket.ConnectAsync(endpoint);
            return new NetworkStream(socket, ownsSocket: true);
        }
    });
    private static readonly ILogger<WolfApi> Logger = Main.GetLogger<WolfApi>();
    public static string SessionId { get; private set; } = "";

#nullable disable
    public static Profile Profile { get; set; } = null;
    private static WolfApi _singleton = null;
#nullable enable

    private static bool _startedListening = false;

    public static WolfApi Singleton
    {
        get
        {
            _singleton ??= new WolfApi();
            return _singleton;
        }
    }
    public static void Init()
    {
        _singleton ??= new WolfApi();
    }
    private WolfApi()
    {
        SessionId = System.Environment.GetEnvironmentVariable("WOLF_SESSION_ID") ?? "";
        if (SessionId == "")
        {
            Logger.LogWarning("session_id not found!");
            SessionId = "123456789";
        }
        ApiEvent += FilterApiEvents;
        JsonOptions.Converters.Add(new StaticFactoryConverter<App>());
        StartListenToApiEvents();
    }

    private void InvokeEvent<T>(EventHandler<T>? handler, string jsonArgs)
    {
        var argObj = JsonSerializer.Deserialize<T>(jsonArgs, JsonOptions);
        if (argObj is null)
        {
            Logger.LogError("{0} cant be Deserialized to {1}", jsonArgs, typeof(T));
            return;
        }
        handler?.Invoke(this, argObj);
    }
    private void FilterApiEvents(string @event, string data)
    {
        var operations = new Dictionary<string, Action<string>> {
            { "DockerPullImageEndEvent", (_) => {}},
            { "DockerPullImageStartEvent", (_) => {}},
            { "wolf::core::events::PlugDeviceEvent", (_)=>{}},
            { "wolf::core::events::UnplugDeviceEvent", (_)=>{}},
            { "wolf::core::events::PairSignal", (_)=>{}},
            { "wolf::core::events::StartRunner", (_)=>{}},
            { "wolf::core::events::StreamSession", (_)=>{}},
            { "wolf::core::events::StopStreamEvent", (_)=>{}},
            { "wolf::core::events::VideoSession", (_)=>{}},
            { "wolf::core::events::RTPAudioPingEvent", (_)=>{}},
            { "wolf::core::events::AudioSession", (_)=>{}},
            { "wolf::core::events::IDRRequestEvent", (_)=>{}},
            { "wolf::core::events::RTPVideoPingEvent", (_)=>{}},
            { "wolf::core::events::ResumeStreamEvent", (_)=>{}},
            { "wolf::core::events::PauseStreamEvent", (_)=>{}},
            { "wolf::core::events::SwitchStreamProducerEvents", (_)=>{}},
            { "wolf::core::events::JoinLobbyEvent", InvokeLobbyJoin},
            { "wolf::core::events::LeaveLobbyEvent", InvokeLobbyLeave},
            { "wolf::core::events::CreateLobbyEvent", InvokeLobbyCreated },
            { "wolf::core::events::StopLobbyEvent", InvokeLobbyStopped },
        };

        //var failed = delegate(){ Logger.LogInformation("{Event} - {Data}", @event, data); };

        if (!operations.TryGetValue(@event, out var value))
        {
            Logger.LogWarning("{0} - {1}", @event, data);
            return;
        }

        value(data);
        return;

        void InvokeLobbyCreated(string dataJson) => InvokeEvent(LobbyCreatedEvent, dataJson);
        void InvokeLobbyStopped(string dataJson) => LobbyStoppedEvent?.Invoke(this, dataJson.TrimPrefix("{\"lobby_id\":\"").TrimSuffix("\"}"));
        void InvokeLobbyJoin(string dataJson) => EmitSignalLobbyJoinEvent(dataJson[..dataJson.LastIndexOf(',')].TrimPrefix("{\"lobby_id\":\"").TrimSuffix("\""));
        void InvokeLobbyLeave(string dataJson) => EmitSignalLobbyLeaveEvent(dataJson[..dataJson.LastIndexOf(',')].TrimPrefix("{\"lobby_id\":\"").TrimSuffix("\""));
    }

    
    private async void StartListenToApiEvents()
    {
        if (_startedListening)
            return;

        _startedListening = true;

        await Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    var stream = await _httpClient.GetStreamAsync($"{Api}/events");
                    var eventType = string.Empty;
                    using var reader = new StreamReader(stream);
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line is null) continue;
                        if (line == ":keepalive") continue;

                        if (line.StartsWith("event:"))
                            eventType = line["event: ".Length..];

                        if (!line.StartsWith("data:")) continue;
                        
                        var data = line["data: ".Length..];
                        EmitSignalApiEventDeferred(eventType, data);
                    }

                    Logger.LogError("Lost connection to the Wolf API SSE. End of Stream.");
                    await Task.Delay(1000);
                }
                catch (HttpRequestException e)
                {
                    Logger.LogError("Failed connecting to the Wolf API: {0}.", e.Message);
                    await QuestionDialogue.OpenDialogue<bool>("Error", $"Failed connecting to the Wolf API:\n {e.Message}.", new()
                    {
                        { "OK", true }
                    });
                    Main.Singleton.GetTree().Quit();
                }
            }
        });
        return;

        void EmitSignalApiEventDeferred(string eventType, string data) => CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.ApiEvent, eventType, data);
    }
    
    private static async Task<string?> PostAsync<T>(string url, T obj)
    {
        try
        {
            string data;
            if (obj is string str)
            {
                data = str;
            }
            else
            {
                data = JsonSerializer.Serialize(obj, JsonOptions);
                if(data == "{}") Logger.LogError("Type {0}, has no JsonIncludeAttribute set!", obj is null ? "NULL" : obj.GetType());
            }

            Logger.LogDebug("API call POST: {0} - {1}", url, data);
            StringContent content = new(data);
            var result = await _httpClient.PostAsync($"{Api}{url}", content);
            var returnData = await result.Content.ReadAsStringAsync();
            Logger.LogDebug("API answer from: {0} - {1}", url, returnData);
            return returnData;
        }
        catch (Exception e)
        {
            Logger.LogError("Error POST request failed: {0} Message: {1}", url, e.Message);
            return null;
        }
    }
    /**
        <summary>
        Static Method <c>GetAsync</c> is a helper that calls <c>url</c> and returns a Deserialized object of a chosen type.
        </summary>
        <param name="url">The url of the API to call</param>
        <returns>
        An object of chosen type containing the data returned by the API.
        </returns>
    */
    private static async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            var result = await _httpClient.GetStringAsync(url);
            Logger.LogDebug("API call GET: {0} - {1}", url, result);
            var data = JsonSerializer.Deserialize<T>(result, JsonOptions);
            if (data is not null) return data;
            Logger.LogError("Could not Deserialize {0} to {1}", result, typeof(T));
            return default;
        }
        catch (Exception e)
        {
            Logger.LogError("Error GET request failed: {0} Message:{1} - {2}", url, e.GetType(), e.Message);
            return default(T);
        }

    }

    [Signal]
    private delegate void ApiEventEventHandler(string eventType, string data);
    [Signal]
    public delegate void ImageUpdatedEventHandler(string imageName);
    [Signal]
    public delegate void ImageAlreadyUptoDateEventHandler(string imageName);
    [Signal]
    public delegate void ImagePullProgressEventHandler(string imageName, double progress);

    [Signal]
    public delegate void LobbyJoinEventEventHandler(string lobbyId);    
    [Signal]
    public delegate void LobbyLeaveEventEventHandler(string lobbyId);
}