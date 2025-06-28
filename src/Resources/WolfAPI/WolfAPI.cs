using Godot;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WolfUI;

namespace Resources.WolfAPI;

/* Test for directly serializing and deserializing Godot Objects like Nodes.
    public sealed class OptinJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

            List<JsonPropertyInfo> prop_to_remove = [.. jsonTypeInfo.Properties
                    .Where(prop => !prop.AttributeProvider.IsDefined(typeof(JsonIncludeAttribute), false))];

            foreach (var prop in prop_to_remove)
            {
                jsonTypeInfo.Properties.Remove(prop);
            }

            return jsonTypeInfo;
        }
    }
*/

[GlobalClass]
public partial class WolfAPI : Resource
{
    public event EventHandler<Lobby> LobbyCreatedEvent;
    public event EventHandler<string> LobbyStoppedEvent;
    private static readonly System.Net.Http.HttpClient _httpClient = new(new SocketsHttpHandler
    {
        ConnectCallback = async (context, token) =>
        {
            string endpoint_path = System.Environment.GetEnvironmentVariable("WOLF_SOCKET_PATH");
            endpoint_path ??= "/var/run/wolf/wolf.sock";
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            var endpoint = new UnixDomainSocketEndPoint(endpoint_path);
            await socket.ConnectAsync(endpoint);
            return new NetworkStream(socket, ownsSocket: true);
        }
    });
    private static readonly ILogger<WolfAPI> Logger = WolfUI.Main.GetLogger<WolfAPI>();
    private static string SessionId = "";
    public static string session_id {get{ return SessionId;}}
    public static Profile Profile = null;
    private static bool StartedListening = false;
    private static WolfAPI _Singleton = null;
    public static WolfAPI Singleton
    {
        get
        {
            _Singleton ??= new();
            return _Singleton;
        }
    }
    public static void Init()
    {
        _Singleton ??= new();
    }
    private WolfAPI()
    {
        SessionId = System.Environment.GetEnvironmentVariable("WOLF_SESSION_ID");
        if (session_id == null)
        {
            Logger.LogWarning("session_id not found!");
            //GD.Print("session_id not found!");
            SessionId = "123456789";
        }
        APIEvent += FilterAPIEvents;
        StartListenToAPIEvents();
    }
    private void InvokeEvent<T>(EventHandler<T> handler, string json_args)
    {
        handler?.Invoke(this, JsonSerializer.Deserialize<T>(json_args));
    }
    private void FilterAPIEvents(string @event, string data)
    {
        void InvokeLobbyStopped(string data) => LobbyStoppedEvent?.Invoke(this, data.TrimPrefix("{\"lobby_id\":\"").TrimSuffix("\"}"));
        void InvokeLobbyCreated(string data) => InvokeEvent(LobbyCreatedEvent, data);

        var Operations = new Dictionary<string, Action<string>> {
            { "wolf::core::events::PlugDeviceEvent", (data)=>{}},
            { "wolf::core::events::UnplugDeviceEvent", (data)=>{}},
            { "wolf::core::events::PairSignal", (data)=>{}},
            { "wolf::core::events::StartRunner", (data)=>{}},
            { "wolf::core::events::StreamSession", (data)=>{}},
            { "wolf::core::events::StopStreamEvent", (data)=>{}},
            { "wolf::core::events::VideoSession", (data)=>{}},
            { "wolf::core::events::RTPAudioPingEvent", (data)=>{}},
            { "wolf::core::events::AudioSession", (data)=>{}},
            { "wolf::core::events::IDRRequestEvent", (data)=>{}},
            { "wolf::core::events::RTPVideoPingEvent", (data)=>{}},
            { "wolf::core::events::ResumeStreamEvent", (data)=>{}},
            { "wolf::core::events::PauseStreamEvent", (data)=>{}},
            { "wolf::core::events::SwitchStreamProducerEvents", (data)=>{}},
            { "wolf::core::events::JoinLobbyEvent", (data)=>{}},
            { "wolf::core::events::LeaveLobbyEvent", (data)=>{}},
            { "wolf::core::events::CreateLobbyEvent", InvokeLobbyCreated },
            { "wolf::core::events::StopLobbyEvent", InvokeLobbyStopped },
        };

        //var failed = delegate(){ Logger.LogInformation("{Event} - {Data}", @event, data); };

        if (!Operations.TryGetValue(@event, out var value))
        {
            Logger.LogInformation("{0} - {1}", @event, data);
            return;
        }

        value(data);
    }
    public static async Task<Texture2D> GetAppIcon(App app)
    {
        if (app.icon_png_path == null) // no image set, get default from github
        {
            if (app.runner == null || !app.runner.image.Contains("ghcr.io/games-on-whales/"))
                return null;

            var name = app.runner.image.TrimPrefix("ghcr.io/games-on-whales/");//.TrimSuffix(":edge");
            int idx = name.LastIndexOf(':');
            if(idx >= 0)
                name = name[..idx];

            string user = System.Environment.GetEnvironmentVariable("USER");
            user = (user == "root" || user == null) ? "retro" : user;
            string filepath = $"/home/{user}/.wolf-ui/tmp/{name}.png";

            Image image;
            
            if (File.Exists(filepath))
            {
                image = Image.LoadFromFile(filepath);
                return ImageTexture.CreateFromImage(image);
            }

            System.Net.Http.HttpClient httpClient = new();
            var result = await httpClient.GetByteArrayAsync($"https://games-on-whales.github.io/wildlife/apps/{name}/assets/icon.png");
            image = new();
            image.LoadPngFromBuffer(result);
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            image.SavePng(filepath);
            Texture2D texture2D = ImageTexture.CreateFromImage(image);
            return texture2D;
        }
        else
        {
            bool isURL = Uri.TryCreate(app.icon_png_path, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (isURL)
            {
                try
                {
                    System.Net.Http.HttpClient httpClient = new();
                    var result = await httpClient.GetByteArrayAsync(app.icon_png_path);
                    Image image = new();
                    image.LoadPngFromBuffer(result);
                    return ImageTexture.CreateFromImage(image);
                }
                catch (HttpRequestException e)
                {
                    Logger.LogError("Could not access image url: {0}: {1}", app.icon_png_path, e.StatusCode);
                    return null;
                }
            }
            else
            {
                Image image = Image.LoadFromFile(app.icon_png_path);
                var texture = ImageTexture.CreateFromImage(image);
                return texture;
            }
        }
    }
    public async void StartListenToAPIEvents()
    {
        if (StartedListening)
            return;

        StartedListening = true;
        void EmitSignalAPIEventDeferred(string eventType, string data) => CallDeferred(MethodName.EmitSignal, SignalName.APIEvent, eventType, data);

        await Task.Run(new(async () =>
        {
            while (true)
            {
                try
                {
                    var stream = await _httpClient.GetStreamAsync("http://localhost/api/v1/events");
                    string eventType = "";
                    using var reader = new StreamReader(stream);
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line is null)
                            continue;

                        if (line == ":keepalive")
                            continue;

                        if (line.StartsWith("event:"))
                            eventType = line["event: ".Length..];

                        if (line.StartsWith("data:"))
                        {
                            var data = line["data: ".Length..];
                            EmitSignalAPIEventDeferred(eventType, data);
                        }
                    }

                    Logger.LogError("Lost connection to the Wolf API. End of Stream.");
                    await Task.Delay(1000);
                }
                catch (HttpRequestException e)
                {
                    Logger.LogError("Failed connecting to the Wolf API: {0}; Retrying in 5s.", e.Message);
                    await Task.Delay(5000);
                }
            }
        }));
    }
    public static async Task<List<App>> GetApps(Profile used_profile)
    {
        List<Profile> profiles = await GetProfiles();

        foreach(var profile in profiles)
        {
            if(profile.id == used_profile.id)
                return profile.apps;
        }
        Logger.LogError("Profile: {0} not found", used_profile.name);
        //GD.Print($"Profile: {used_profile.name} not found");

        return [];
    }
    public static async Task<List<Profile>> GetProfiles()
    {
        Profiles profiles = await GetAsync<Profiles>("http://localhost/api/v1/profiles");

        if (!profiles.success)
        {
            Logger.LogError("Error retrieving Profiles");
            //GD.Print("Error retrieving Profiles");
            return [];
        }

        return profiles.profiles;
    }
    public static async Task<List<Client>> GetClients()
    {
        Clients wolfClients = await GetAsync<Clients>("http://localhost/api/v1/clients");

        if (wolfClients?.success == true)
            return wolfClients.clients;
        return [];
    }
    public static async Task StartApp(Runner runner, bool joinable = false)
    {
        var starter = new Starter()
        {
            stop_stream_when_over = false,
            session_id = session_id,
            runner = runner
        };
        var result = await PostAsync("http://localhost/api/v1/runners/start", starter);
        //Logger.LogInformation("{0}", result);
        //GD.Print(await result.Content.ReadAsStringAsync());
    }
    public static async Task<List<Lobby>> GetLobbies()
    {
        Lobbies lobbies = await GetAsync<Lobbies>("http://localhost/api/v1/lobbies");
        if(lobbies?.success == true)
            return lobbies.lobbies ?? [];
        return [];
    }
    public static async Task<Session> GetSession()
    {
        var sessions = await WolfAPI.GetAsync<Sessions>("http://localhost/api/v1/sessions");
        Session curr_session = null;
        foreach (var session in sessions?.sessions)
        {
            if (session.client_id == session_id)
            {
                curr_session = session;
                break;
            }
        }

        if (curr_session == null)
        {
            Logger.LogWarning("No owned Session found. Is this run without Wolf?");
            //GD.Print("No owned Session found. Is this run without Wolf?");
            curr_session = new()
            {
                video_width = 1920,
                video_height = 1080,
                video_refresh_rate = 60,
                audio_channel_count = 2,
                client_settings = new()
            };
        }

        return curr_session;
    }
    /**
        <summary>
        Static Method <c>CreateLobby</c> creates a Lobby based on the passed <c>Lobby</c>.
        </summary>
        <param name="lobby">the object defining the lobby that should be created</param>
        <returns>
        A string containing the Lobbies ID.
        </returns>
    */
    public static async Task<string> CreateLobby(Lobby lobby)
    {
        var content = await PostAsync("http://localhost/api/v1/lobbies/create", lobby);
        //var content = await result.Content.ReadAsStringAsync();
        //Logger.LogInformation("called lobbies/create: {0}", content);
        //GD.Print($"called lobbies/create: {content}");
        return JsonSerializer.Deserialize<LobbyCreatedResponse>(content)?.lobby_id;
    }
    public static async Task JoinLobby(string lobby_id, string session_id)
    {
        await JoinLobby(lobby_id, session_id, null);
    }
    public static async Task JoinLobby(string lobby_id, string session_id, List<int> pin)
    {
        var lobbyobj = new LobbyJoin()
        {
            lobby_id = lobby_id,
            moonlight_session_id = session_id,
            pin = pin
        };

        var result = await PostAsync("http://localhost/api/v1/lobbies/join", lobbyobj);
        //Logger.LogInformation("called lobbies/join: {0}", await result.Content.ReadAsStringAsync());
        //GD.Print($"called lobbies/join: {await result.Content.ReadAsStringAsync()}");
    }
    public static async Task LeaveLobby(string lobby_id, string session_id)
    {
        string json = $@"
        {{
            ""lobby_id"": ""{lobby_id}"",
            ""moonlight_session_id"": ""{session_id}""
        }}";

        StringContent content = new(json);
        var result = await _httpClient.PostAsync("http://localhost/api/v1/lobbies/leave", content);
        Logger.LogInformation("{0}", await result.Content.ReadAsStringAsync());
        //GD.Print(await result.Content.ReadAsStringAsync());
    }
    private record StopLobbyRecord
    {
        public string lobby_id { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int> pin { get; set; }
    }
    public static async Task StopLobby(string lobby_id, List<int> pin = null)
    {
        var stop_lobby = new StopLobbyRecord()
        {
            lobby_id = lobby_id,
            pin = pin
        };

        var result = await PostAsync("http://localhost/api/v1/lobbies/stop", stop_lobby);
        //Logger.LogInformation("{0}", await result.Content.ReadAsStringAsync());
    }
    private static async Task<string> PostAsync<T>(string url, T obj)
    {
        string data = JsonSerializer.Serialize<T>(obj);
        Logger.LogDebug("API call POST: {0} - {1}", url, data);
        StringContent content = new(data);
        var result = await _httpClient.PostAsync(url, content);
        var return_data = await result.Content.ReadAsStringAsync();
        Logger.LogDebug("API answer from: {0} - {1}", url, return_data);
        return return_data;
    }
    /**
        <summary>
        Static Method <c>GetAsync</c> is a helper that calls <c>url</c> and returns a Deserialized object of a choosen type.
        </summary>
        <param name="url">The url of the API to call</param>
        <returns>
        A object of choosen type containing the data returned by the API.
        </returns>
    */
    public static async Task<T> GetAsync<T>(string url)
    {
        var result = await _httpClient.GetStringAsync(url);
        Logger.LogDebug("API call GET: {0} - {1}", url, result);
        T data = JsonSerializer.Deserialize<T>(result);
        return data;
    }

    [Signal]
    private delegate void APIEventEventHandler(string eventType, string data);
}