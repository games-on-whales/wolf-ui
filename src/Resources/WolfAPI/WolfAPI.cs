using Godot;
using System;
using WolfUI;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Resources.WolfAPI;

/* Test for directly serializing and deserializing Godot Objects like Nodes. */
public sealed class OptInJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var jsonTypeInfo = base.GetTypeInfo(type, options);

        List<JsonPropertyInfo> propToRemove = [.. jsonTypeInfo.Properties
                .Where(prop => prop.AttributeProvider is not null 
                               && !prop.AttributeProvider.IsDefined(typeof(JsonIncludeAttribute), false))];

        foreach (var prop in propToRemove)
        {
            jsonTypeInfo.Properties.Remove(prop);
        }

        return jsonTypeInfo;
    }
}

public class StaticFactoryConverter<T> : JsonConverter<T>
{
    private readonly MethodInfo _factoryMethod;

    public StaticFactoryConverter(string factoryMethodName = "Restore")
    {
        _factoryMethod = typeof(T).GetMethod(factoryMethodName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new ArgumentException($"No static method named {factoryMethodName} found in type {typeof(T).Name}");
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        
        var obj = (T?)_factoryMethod.Invoke(null, []);
        if (obj is null) throw new JsonException($"Can't Invoke Factory Method: {_factoryMethod.Name}");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return obj;
            
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            
            var propertyName = reader.GetString();

            var property = typeof(T).GetTypeInfo().DeclaredProperties
                    .Where(prop => prop.GetCustomAttribute<JsonIncludeAttribute>() is not null) 
                    .Where(prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>() is not null)
                    .FirstOrDefault(prop => prop.GetCustomAttributesData()
                        .FirstOrDefault(cad => cad.AttributeType == typeof(JsonPropertyNameAttribute))?.ConstructorArguments
                            .First().Value as string == propertyName);
            
            reader.Read();
            if(property is null) continue;
            dynamic? value = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
            property.SetValue(obj, value);
            
        }
        throw new JsonException();
    }
    
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

[GlobalClass]
public partial class WolfApi : Resource
{
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
            //{ "wolf::core::events::JoinLobbyEvent", (_)=>{}},
            //{ "wolf::core::events::LeaveLobbyEvent", (_)=>{}},
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
    }

    public static async Task<Texture2D?> GetIcon(string iconPath, double hCacheDuration = 1.0, int retry = 0)
    {
        if (retry >= 5)
        {
            Logger.LogError("Failed Loading {0} 5 times, skipping", iconPath);
            return null;
        }
        var user = System.Environment.GetEnvironmentVariable("USER") ?? "retro";
        user = user == "root" ? "retro" : user;

        var filepath = $"/home/{user}/.wolf-ui/tmp/icons/{iconPath}.png";


        if (File.Exists(filepath))
        {
            if (File.GetCreationTime(filepath).AddHours(hCacheDuration).CompareTo(DateTime.Now) >= 0)
            {
                var image = Image.LoadFromFile(filepath);
                return ImageTexture.CreateFromImage(image);
            }
            else
            {
                File.Delete(filepath);
            }
        }

        Logger.LogInformation("Requesting icon: {0}", iconPath);

        HttpResponseMessage message;
        try
        {
            message = await _httpClient.GetAsync($"http://localhost/api/v1/utils/get-icon?icon_path={iconPath}");
        }
        catch (HttpRequestException e)
        {
            if(e.InnerException is not null)
                Logger.LogWarning("Icon {0} could not be accessed: {1} - {2} Retrying", iconPath, e.Message, e.InnerException.Message);
            else
                Logger.LogWarning("Icon {0} could not be accessed: {1} Retrying", iconPath, e.Message);
            return await GetIcon(iconPath, hCacheDuration, retry + 1);
        }

        if (message.StatusCode == HttpStatusCode.OK)
        {
            Image image = new();
            var error = await image.LoadImageFromHttpResonseMessage(message);
            if (error != Error.Ok)
            {
                if (error == Error.FileUnrecognized)
                {
                    return null;
                }

                Logger.LogError("Icon {0} could not be decoded properly, Retrying", iconPath);
                return await GetIcon(iconPath, hCacheDuration, retry + 1);
            }

            var directoryPath = Path.GetDirectoryName(filepath);
            if (directoryPath is not null)
            {
                Directory.CreateDirectory(directoryPath);
                image.SavePng(filepath);
            }
            var texture = ImageTexture.CreateFromImage(image);
            return texture;
        }
        Logger.LogError("Could not access image: {0}: {1}", iconPath, message.StatusCode);
        return null;
    }

    public static async Task<Texture2D?> GetAppIcon(App app)
    {
        if (app.IconPngPath is not null) // no image set, get default from GitHub
            return await GetIcon(app.IconPngPath);

        if (app.Runner?.Image is null || !app.Runner.Image.Contains("ghcr.io/games-on-whales/"))
            return null;

        var name = app.Runner.Image.TrimPrefix("ghcr.io/games-on-whales/");//.TrimSuffix(":edge");
        var idx = name.LastIndexOf(':');
        if (idx >= 0)
            name = name[..idx];

        return await GetIcon($"https://games-on-whales.github.io/wildlife/apps/{name}/assets/icon.png");
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
                    var stream = await _httpClient.GetStreamAsync("http://localhost/api/v1/events");
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

    //TODO Create Class for the Inspect return. 
    public static async Task InspectImage(string imageName)
    {
        var response = await _httpClient.GetAsync($"http://localhost/api/v1/docker/images/inspect?image_name={imageName}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }
        if (response.StatusCode == HttpStatusCode.OK)
        {

        }
        return;
    }

    public static async Task<bool> IsImageOnDisk(string imageName, int retryCount = 0)
    {
        var cacheKey = $"http://localhost/api/v1/docker/images/inspect?image_name={imageName}";

        if (_cache.Contains(cacheKey))
        {
            return _cache.Get(cacheKey) as string != "";
        }

        if (retryCount >= 5)
        {
            Logger.LogError("Api call failed 5 times: /docker/images/inspect?image_name={0}, {1}... ABORT", imageName);
            return false;
        }

        try
        {
            var response = await _httpClient.GetAsync(cacheKey);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _cache.Add(cacheKey, "", WolfAPICachePolicy);
                return false;
            }
            var str = await response.Content.ReadAsStringAsync();
            _cache.Add(cacheKey, str, WolfAPICachePolicy);
            return true;
        }
        catch (HttpRequestException e)
        {
            Logger.LogWarning("Api call failed: /docker/images/inspect?image_name={0}, {1}... Retrying", imageName, e.Message);
            return await IsImageOnDisk(imageName, retryCount + 1);
        }
    }

    private sealed record PullImageResponse
    {
        public bool? success { get; init; }
        public string? layer_id { get; init; }
        public long current_progress { get; init; }
        public long total { get; init; }
    }

    public static void PullImage(string imageName)
    {
        _ = Task.Run(async () =>
        {
            var json = $$"""
                         {
                             "image_name": "{{imageName}}"
                         }
                         """;

            EmitSignalDeferredImagePullProgress(imageName, 0.0);

            Logger.LogInformation("Pulling image: {0}", imageName);

            var reqMsg = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/v1/docker/images/pull")
            {
                Content = new StringContent(json)
            };

            var response = await _httpClient.SendAsync(reqMsg, HttpCompletionOption.ResponseHeadersRead);
            Logger.LogInformation("Pull request: {0}", response.StatusCode);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.LogError("Cant Pull Image: {0}, {1}", imageName, response.StatusCode);
            }

            var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            Dictionary<string, PullImageResponse> layerSizes = [];
            var isUnpacking = false;
            long lastCurrent = 0;
            var hasDownloaded = false;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line is null)
                    continue;

                var parsed = JsonSerializer.Deserialize<PullImageResponse>(line, JsonOptions);
                if (parsed is null)
                    continue;


                if (parsed.success is not null && parsed.success == true)
                {
                    var cacheKey = $"http://localhost/api/v1/docker/images/inspect?image_name={imageName}";
                    if (_cache.Contains(cacheKey))
                        _cache.Remove(cacheKey);
                    _cache.Add(cacheKey, "exists", WolfAPICachePolicy);

                    if (hasDownloaded)
                        EmitSignalDeferredImageUpdated(imageName);
                    else
                        EmitSignalDeferredImageAlreadyUptoDate(imageName);

                    Logger.LogInformation("Image: {0} {1}", imageName, hasDownloaded ? "was Updated" : "is already up to Date");
                    return;
                }

                hasDownloaded = true;

                if (parsed.layer_id is null)
                    continue;

                layerSizes[parsed.layer_id] = new PullImageResponse
                {
                    layer_id = parsed.layer_id,
                    current_progress = parsed.current_progress,
                    total = parsed.total
                };

                long current = 0;
                long total = 0;

                foreach (var kv in layerSizes)
                {
                    current += kv.Value.current_progress;
                    total += kv.Value.total;
                }

                var sizeTotal = total;
                total *= 2;

                if (total <= 1000) continue;
                
                if (lastCurrent > 0 && lastCurrent > current + 0.3 * lastCurrent)
                    isUnpacking = true;
                lastCurrent = current;
                var percentProgress = 100.0 * (current + (isUnpacking ? sizeTotal : 0)) / total;
                EmitSignalDeferredImagePullProgress(imageName, percentProgress);
            }

            return;

            void EmitSignalDeferredImagePullProgress(string argImageName, double progress) => Singleton.CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.ImagePullProgress, argImageName, progress);
            void EmitSignalDeferredImageAlreadyUptoDate(string argImageName) => Singleton.CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.ImageAlreadyUptoDate, argImageName);
            void EmitSignalDeferredImageUpdated(string argImageName) => Singleton.CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.ImageUpdated, argImageName);
        });
    }

    public static async Task<List<App>> GetApps(Profile usedProfile)
    {
        List<Profile> profiles = await GetProfiles();

        foreach (var profile in profiles)
        {
            if (profile.Id is not null && usedProfile.Id is not null && profile.Id == usedProfile.Id)
                return profile.Apps ?? [];
        }
        Logger.LogError("Profile: {0} not found", usedProfile.Name ?? "NULL");
        return [];
    }

    public static async Task<AppsRequest?> GetApps()
    {
        var ret = await GetAsync<AppsRequest>("http://localhost/api/v1/apps");
        return ret;
    }

    public static async Task<List<Profile>> GetProfiles()
    {
        var profiles = await GetAsync<ProfilesResponse>("http://localhost/api/v1/profiles");

        if (profiles is null || !profiles.Success)
        {
            Logger.LogError("Error retrieving Profiles");
            return [];
        }

        return profiles.Profiles ?? [];
    }
    public static async Task<List<Client>> GetClients()
    {
        var wolfClients = await GetAsync<ClientsRequest>("http://localhost/api/v1/clients");

        if (wolfClients?.Success == true)
            return wolfClients.Clients ?? [];
        return [];
    }
    public static async Task StartApp(Runner runner, bool joinable = false)
    {
        var starter = new Starter()
        {
            StopStreamWhenOver = false,
            SessionId = SessionId,
            Runner = runner
        };
        var result = await PostAsync("http://localhost/api/v1/runners/start", starter);
    }

    public static async void StopSession(string sessionId)
    {
        var url = "http://localhost/api/v1/session/stop";
        try
        {
            var data = $$"""
                         {
                             "session_id": "{{sessionId}}"
                         }
                         """;
            Logger.LogDebug("API call POST: {0} - {1}", url, data);
            StringContent content = new(data);
            var result = await _httpClient.PostAsync(url, content);
            var returnData = await result.Content.ReadAsStringAsync();
            Logger.LogDebug("API answer from: {0} - {1}", url, returnData);
        }
        catch (Exception e)
        {
            Logger.LogError("Error POST request failed: {0} Message: {1}", url, e.Message);
        }
    }

    public static async Task<List<Lobby>> GetLobbies()
    {
        var lobbies = await GetAsync<LobbiesResponse>("http://localhost/api/v1/lobbies");
        if (lobbies is null) return [];
        if (lobbies.Success) return lobbies.Lobbies ?? [];
        return [];
    }
    public static async Task<Session?> GetSession()
    {
        var sessions = await WolfApi.GetAsync<SessionsResponse>("http://localhost/api/v1/sessions");
        if (sessions?.Sessions is null) return null;
        var currSession = sessions.Sessions.FirstOrDefault(session => session.ClientId == SessionId);
        if (currSession is not null) return currSession;
        Logger.LogWarning("No owned Session found. Is this run without Wolf?");

        return new Session
        {
            VideoWidth = 1920,
            VideoHeight = 1080,
            VideoRefreshRate = 60,
            AudioChannelCount = 2,
            ClientSettings = new ClientSettings()
        };
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
    public static async Task<string?> CreateLobby(Lobby lobby)
    {
        var content = await PostAsync("http://localhost/api/v1/lobbies/create", lobby);
        return content is null ? null : JsonSerializer.Deserialize<LobbyCreatedResponse>(content, JsonOptions)?.LobbyId;
    }
    public static async Task<ErrorResponse?> JoinLobby(string lobbyId, string sessionId)
    {
        return await JoinLobby(lobbyId, sessionId, null);
    }
    public static async Task<ErrorResponse?> JoinLobby(string lobbyId, string sessionId, List<int>? pin)
    {
        var lobby = new LobbyJoin()
        {
            LobbyId = lobbyId,
            MoonlightSessionId = sessionId,
            Pin = pin
        };

        var result = await PostAsync("http://localhost/api/v1/lobbies/join", lobby);
        return result is null ? null : JsonSerializer.Deserialize<ErrorResponse>(result, JsonOptions);
    }
    public static async Task LeaveLobby(string lobbyId, string sessionId)
    {
        var json = $$"""
                     {
                         "lobby_id": "{{lobbyId}}",
                         "moonlight_session_id": "{{sessionId}}"
                     }
                     """;

        StringContent content = new(json);
        var result = await _httpClient.PostAsync("http://localhost/api/v1/lobbies/leave", content);
        Logger.LogInformation("{0}", await result.Content.ReadAsStringAsync());
    }
    private class StopLobbyRecord
    {
        [JsonInclude, JsonPropertyName("lobby_id")]
        public required string LobbyId { get; set; }

        [JsonInclude, JsonPropertyName("pin"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int>? Pin { get; set; }
    }
    public static async Task StopLobby(string lobbyId, List<int>? pin = null)
    {
        var stopLobby = new StopLobbyRecord()
        {
            LobbyId = lobbyId,
            Pin = pin
        };

        var result = await PostAsync("http://localhost/api/v1/lobbies/stop", stopLobby);
        //return result is null ? null : JsonSerializer.Deserialize<ErrorResponse>(result, JsonOptions);
    }
    private static async Task<string?> PostAsync<T>(string url, T obj)
    {
        try
        {
            var data = JsonSerializer.Serialize(obj, JsonOptions);
            if(data == "{}") Logger.LogError("Type {0}, has no JsonIncludeAttribute set!", obj.GetType());
            Logger.LogDebug("API call POST: {0} - {1}", url, data);
            StringContent content = new(data);
            var result = await _httpClient.PostAsync(url, content);
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
}