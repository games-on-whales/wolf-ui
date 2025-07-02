using Godot;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.Caching;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Resources.WolfAPI;

[GlobalClass]
public partial class WolfAPI : Resource
{
    private static readonly MemoryCache _cache = MemoryCache.Default;

    private static CacheItemPolicy _cachePolicy
    {
        get
        {
            Random random = new();
            var variance = random.NextDouble() * 50;

            var cacheItemPolicy = new CacheItemPolicy()
            {
                //Set your Cache expiration.
                AbsoluteExpiration = DateTime.Now.AddSeconds(20 + variance)
            };
            return cacheItemPolicy;
        }
    }

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
    public static string session_id { get { return SessionId; } }
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
            Logger.LogWarning("{0} - {1}", @event, data);
            return;
        }

        value(data);
    }
    public static async Task<Texture2D> GetAppIcon(App app, double h_cache_duration = 1.0, int retrys = 0)
    {
        if (retrys >= 5)
        {
            Logger.LogError("Failed Loading {0} 5 times, skipping", app.icon_png_path);
        }

        string user = System.Environment.GetEnvironmentVariable("USER");
        user = (user == "root" || user == null) ? "retro" : user;

        if (app.icon_png_path == null) // no image set, get default from github
        {
            if (app.runner == null || !app.runner.image.Contains("ghcr.io/games-on-whales/"))
                return null;

            var name = app.runner.image.TrimPrefix("ghcr.io/games-on-whales/");//.TrimSuffix(":edge");
            int idx = name.LastIndexOf(':');
            if (idx >= 0)
                name = name[..idx];

            string filepath = $"/home/{user}/.wolf-ui/tmp/{name}.png";

            Image image;

            if (File.Exists(filepath))
            {
                if (File.GetCreationTime(filepath).AddHours(h_cache_duration).CompareTo(DateTime.Now) >= 0)
                {
                    image = Image.LoadFromFile(filepath);
                    return ImageTexture.CreateFromImage(image);
                }
                else
                {
                    File.Delete(filepath);
                }
            }

            Logger.LogInformation("Requesting icon for: {0}", app.title);
            var wildlife_url = $"https://games-on-whales.github.io/wildlife/apps/{name}/assets/icon.png";
            var message = await _httpClient.GetAsync($"http://localhost/api/v1/utils/get-icon?icon_path={wildlife_url}");

            if (message.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var result = await message.Content.ReadAsByteArrayAsync();
                image = new();
                image.LoadPngFromBuffer(result);
                Directory.CreateDirectory(Path.GetDirectoryName(filepath));
                image.SavePng(filepath);
                var texture = ImageTexture.CreateFromImage(image);
                return texture;
            }
            Logger.LogError("Could not access image url: {0}: {1}", wildlife_url, message.StatusCode);
            return null;
        }
        else
        {
            string filepath = $"/home/{user}/.wolf-ui/tmp/{app.icon_png_path}.png";

            Image image;

            if (File.Exists(filepath))
            {
                if (File.GetCreationTime(filepath).AddHours(h_cache_duration).CompareTo(DateTime.Now) >= 0)
                {
                    image = Image.LoadFromFile(filepath);
                    return ImageTexture.CreateFromImage(image);
                }
                else
                {
                    File.Delete(filepath);
                }
            }

            HttpResponseMessage message;
            try
            {
                Logger.LogInformation("Requesting icon for: {0}", app.title);
                message = await _httpClient.GetAsync($"http://localhost/api/v1/utils/get-icon?icon_path={app.icon_png_path}");
            }
            catch (HttpRequestException e)
            {
                Logger.LogWarning("Icon for {0} could not be accessed: {1} - {2} Retrying", app.title, e.Message, e.InnerException.Message);
                return await GetAppIcon(app, h_cache_duration, retrys + 1);
            }

            if (message.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var result = await message.Content.ReadAsByteArrayAsync();
                image = new();

                Engine.PrintErrorMessages = false;
                if (image.LoadPngFromBuffer(result) != Error.Ok)
                {
                    Logger.LogError("Icon for {0} could not be decoded properly, Retrying", app.title);
                    return await GetAppIcon(app, h_cache_duration, retrys + 1);
                }
                Engine.PrintErrorMessages = true;

                Directory.CreateDirectory(Path.GetDirectoryName(filepath));
                image.SavePng(filepath);

                var texture = ImageTexture.CreateFromImage(image);
                if (texture is null)
                {
                    Logger.LogError("Icon for {0} could not be decoded properly", app.title);
                    return default;
                }
                return texture;
            }
            Logger.LogError("Could not access image url: {0}: {1}", app.icon_png_path, message.StatusCode);
            return null;
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

                    Logger.LogError("Lost connection to the Wolf API SSE. End of Stream.");
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

    //TODO Create Class for the Inspect return. 
    public static async Task InspectImage(string image_name)
    {
        var response = await _httpClient.GetAsync($"http://localhost/api/v1/docker/images/inspect?image_name={image_name}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }
        if (response.StatusCode == HttpStatusCode.OK)
        {

        }
        return;
    }

    public static async Task<bool> IsImageOnDisk(string image_name, int retry_count = 0)
    {
        string cache_key = $"http://localhost/api/v1/docker/images/inspect?image_name={image_name}";

        if (_cache.Contains(cache_key))
        {
            return _cache.Get(cache_key) as string != "";
        }

        if (retry_count >= 5)
        {
            Logger.LogError("Api call failed 5 times: /docker/images/inspect?image_name={0}, {1}... ABORT", image_name);
            return false;
        }

        try
        {
            var response = await _httpClient.GetAsync(cache_key);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _cache.Add(cache_key, "", _cachePolicy);
                return false;
            }
            var str = await response.Content.ReadAsStringAsync();
            _cache.Add(cache_key, str, _cachePolicy);
            return true;
        }
        catch (HttpRequestException e)
        {
            Logger.LogWarning("Api call failed: /docker/images/inspect?image_name={0}, {1}... Retrying", image_name, e.Message);
            return await IsImageOnDisk(image_name, retry_count + 1);
        }
    }

    private sealed record PullImageResponse
    {
        public bool? success { get; set; }
        public string layer_id { get; set; }
        public long current_progress { get; set; }
        public long total { get; set; }
    }

    public static void PullImage(string image_name)
    {
        _ = Task.Run(new(async () =>
        {
            void EmitSignalDefered_ImageUpdated(string image_name) => Singleton.CallDeferred(MethodName.EmitSignal, SignalName.ImageUpdated, image_name);
            void EmitSignalDefered_ImageAlreadyUptoDate(string image_name) => Singleton.CallDeferred(MethodName.EmitSignal, SignalName.ImageAlreadyUptoDate, image_name);
            void EmitSignalDefered_ImagePullProgress(string image_name, double progress) => Singleton.CallDeferred(MethodName.EmitSignal, SignalName.ImagePullProgress, image_name, progress);

            var json = $@"
                {{
                    ""image_name"": ""{image_name}""
                }}
            ";

            EmitSignalDefered_ImagePullProgress(image_name, 0.0);

            Logger.LogInformation("Pulling image: {0}", image_name);

            var req_msg = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/v1/docker/images/pull")
            {
                Content = new StringContent(json)
            };

            var response = await _httpClient.SendAsync(req_msg, HttpCompletionOption.ResponseHeadersRead);
            Logger.LogInformation("Pull request: {0}", response.StatusCode);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.LogError("Cant Pull Image: {0}, {1}", image_name, response.StatusCode);
            }

            var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            Dictionary<string, PullImageResponse> LayerSizes = [];
            bool isUnpacking = false;
            long lastCurrent = 0;
            bool hasDownloaded = false;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                var parsed = JsonSerializer.Deserialize<PullImageResponse>(line);

                if (parsed.success is not null && parsed.success == true)
                {
                    string cache_key = $"http://localhost/api/v1/docker/images/inspect?image_name={image_name}";
                    if (_cache.Contains(cache_key))
                        _cache.Remove(cache_key);
                    _cache.Add(cache_key, "exists", _cachePolicy);

                    if (hasDownloaded)
                        EmitSignalDefered_ImageUpdated(image_name);
                    else
                        EmitSignalDefered_ImageAlreadyUptoDate(image_name);

                    Logger.LogInformation("Image: {0} {1}", image_name, hasDownloaded ? "was Updated" : "is already up to Date");
                    return;
                }

                hasDownloaded = true;

                LayerSizes[parsed.layer_id] = new()
                {
                    layer_id = parsed.layer_id,
                    current_progress = parsed.current_progress,
                    total = parsed.total
                };

                long current = 0;
                long total = 0;

                foreach (var kv in LayerSizes)
                {
                    current += kv.Value.current_progress;
                    total += kv.Value.total;
                }

                var sizeTotal = total;
                total *= 2;

                if (total != 0 || total > 1000)
                {
                    if (lastCurrent > 0 && lastCurrent > current + 0.3 * lastCurrent)
                        isUnpacking = true;
                    lastCurrent = current;
                    double percent_progress = 100.0 * (current + (isUnpacking ? sizeTotal : 0)) / total;
                    EmitSignalDefered_ImagePullProgress(image_name, percent_progress);
                }
            }
        }));
    }

    public static async Task<List<App>> GetApps(Profile used_profile)
    {
        List<Profile> profiles = await GetProfiles();

        foreach (var profile in profiles)
        {
            if (profile.id == used_profile.id)
                return profile.apps;
        }
        Logger.LogError("Profile: {0} not found", used_profile.name);
        return [];
    }

    public static async Task<Apps> GetApps()
    {
        return await GetAsync<Apps>("http://localhost/api/v1/apps");
    }

    public static async Task<List<Profile>> GetProfiles()
    {
        Profiles profiles = await GetAsync<Profiles>("http://localhost/api/v1/profiles");

        if (!profiles.success)
        {
            Logger.LogError("Error retrieving Profiles");
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
    }
    public static async Task<List<Lobby>> GetLobbies()
    {
        Lobbies lobbies = await GetAsync<Lobbies>("http://localhost/api/v1/lobbies");
        if (lobbies?.success == true)
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
    }
    private static async Task<string> PostAsync<T>(string url, T obj)
    {
        try
        {
            string data = JsonSerializer.Serialize<T>(obj);
            Logger.LogDebug("API call POST: {0} - {1}", url, data);
            StringContent content = new(data);
            var result = await _httpClient.PostAsync(url, content);
            var return_data = await result.Content.ReadAsStringAsync();
            Logger.LogDebug("API answer from: {0} - {1}", url, return_data);
            return return_data;
        }
        catch (Exception e)
        {
            Logger.LogError("Error POST request failed: {0} Message: {1}", url, e.Message);
            return default;
        }
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
        try
        {
            var result = await _httpClient.GetStringAsync(url);
            Logger.LogDebug("API call GET: {0} - {1}", url, result);
            T data = JsonSerializer.Deserialize<T>(result);
            return data;
        }
        catch (Exception e)
        {
            Logger.LogError("Error GET request failed: {0} Message: {1}", url, e.Message);
            return default(T);
        }

    }

    [Signal]
    private delegate void APIEventEventHandler(string eventType, string data);
    [Signal]
    public delegate void ImageUpdatedEventHandler(string image_name);
    [Signal]
    public delegate void ImageAlreadyUptoDateEventHandler(string image_name);
    [Signal]
    public delegate void ImagePullProgressEventHandler(string image_name, double progress);
}