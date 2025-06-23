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
/*
    public readonly static JsonSerializerOptions JsonOptions = new()
    {
        TypeInfoResolver = new OptinJsonTypeInfoResolver()
    };
*/
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

    [Signal]
    public delegate void StartRunnerEventHandler(string data);
    [Signal]
    public delegate void LobbyStoppedEventHandler(string lobby);
    [Signal]
    public delegate void LobbyCreatedEventHandler(string lobby);

    private void FilterAPIEvents(string @event, string data)
    {
        var error = @event switch
        {
            "wolf::core::events::StartRunner" => Error.DoesNotExist,
            "wolf::core::events::StreamSession" => Error.DoesNotExist,
            "wolf::core::events::StopStreamEvent" => Error.DoesNotExist,
            "wolf::core::events::VideoSession" => Error.DoesNotExist,
            "wolf::core::events::RTPAudioPingEvent" => Error.DoesNotExist,
            "wolf::core::events::AudioSession" => Error.DoesNotExist,
            "wolf::core::events::IDRRequestEvent" => Error.DoesNotExist,
            "wolf::core::events::RTPVideoPingEvent" => Error.DoesNotExist,
            "wolf::core::events::ResumeStreamEvent" => Error.DoesNotExist,
            "wolf::core::events::PauseStreamEvent" => Error.DoesNotExist,
            "wolf::core::events::SwitchStreamProducerEvents" => Error.DoesNotExist,
            "wolf::core::events::JoinLobbyEvent" => Error.DoesNotExist,
            "wolf::core::events::LeaveLobbyEvent" => Error.DoesNotExist,
            "wolf::core::events::UnplugDeviceEvent" => Error.DoesNotExist,
            "wolf::core::events::PlugDeviceEvent" => Error.DoesNotExist,
            "wolf::core::events::CreateLobbyEvent" => EmitSignal(SignalName.LobbyCreated, data),
            "wolf::core::events::StopLobbyEvent" => EmitSignal(SignalName.LobbyStopped, data.TrimPrefix("{\"lobby_id\":\"").TrimSuffix("\"}")),
            _ => Error.Unconfigured,
        };
        if(error == Error.Unconfigured)
        {
            Logger.LogInformation("{0} - {1}", @event, data);
            //GD.Print($"{@event} - {data}");
        }
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

            Image image;
            if (isURL)
            {
                System.Net.Http.HttpClient httpClient = new();
                var result = await httpClient.GetByteArrayAsync(app.icon_png_path);
                image = new();
                image.LoadPngFromBuffer(result);
            }
            else
            {
                image = Image.LoadFromFile(app.icon_png_path);
            }

            var texture = ImageTexture.CreateFromImage(image);
            return texture;
        }
    }

    public async void StartListenToAPIEvents()
    {
        if(StartedListening)
            return;

        StartedListening = true;

        await Task.Run(new(async () => { 
            var stream = await _httpClient.GetStreamAsync("http://localhost/api/v1/events");
            string eventType = "";
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line == ":keepalive")
                    continue;

                if (line.StartsWith("event:"))
                    eventType = line.TrimPrefix("event: ");

                if (line.StartsWith("data:"))
                {
                    var data = line.TrimPrefix("data: ");
                    EmitSignal(SignalName.APIEvent, eventType, data);
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
        Logger.LogInformation("Profile: {0} not found", used_profile.name);
        //GD.Print($"Profile: {used_profile.name} not found");

        return [];
    }

    public static async Task<List<Profile>> GetProfiles()
    {
        Profiles profiles = await GetAsync<Profiles>("http://localhost/api/v1/profiles");

        if (!profiles.success)
        {
            Logger.LogInformation("Error retrieving Profiles");
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
        Logger.LogInformation("{0}", await result.Content.ReadAsStringAsync());
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
        var result = await PostAsync("http://localhost/api/v1/lobbies/create", lobby);
        var content = await result.Content.ReadAsStringAsync();
        Logger.LogInformation("called lobbies/create: {0}", content);
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
        Logger.LogInformation("called lobbies/join: {0}", await result.Content.ReadAsStringAsync());
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
        Logger.LogInformation("{0}", await result.Content.ReadAsStringAsync());
    }

    private static async Task<HttpResponseMessage> PostAsync<T>(string url, T obj)
    {
        string data = JsonSerializer.Serialize<T>(obj);
        StringContent content = new(data);
        var result = await _httpClient.PostAsync(url, content);
        return result;
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
        //GD.Print(result);
        T data = JsonSerializer.Deserialize<T>(result);
        return data;
    }

    [Signal]
    private delegate void APIEventEventHandler(string eventType, string data);

}