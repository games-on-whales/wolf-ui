
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace Resources.WolfAPI
{
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

        private static string SessionId = "";
        public static string session_id {get{ return SessionId;}}
        private static bool StartedListening = false;

        public WolfAPI()
        {
            SessionId = System.Environment.GetEnvironmentVariable("WOLF_SESSION_ID");
            if(session_id == null)
            {
                GD.Print("session_id not found!");
                SessionId = "123456789";
            }

            APIEvent += FilterAPIEvents;
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
                "wolf::core::events::CreateLobbyEvent" => EmitSignal(SignalName.LobbyCreated, data),
                "wolf::core::events::StopLobbyEvent" => EmitSignal(SignalName.LobbyStopped, data.TrimPrefix("{\"lobby_id\":\"").TrimSuffix("\"}")),
                _ => Error.Unconfigured,
            };
            if(error == Error.Unconfigured)
            {
                GD.Print($"{@event} - {data}");
            }
        }

        public static async Task<Texture2D> GetAppIcon(App app)
        {
            if(app.runner == null || !app.runner.image.Contains("ghcr.io/games-on-whales/"))
                return null;

            var name = app.runner.image.TrimPrefix("ghcr.io/games-on-whales/").TrimSuffix(":edge");
            System.Net.Http.HttpClient httpClient = new();
            var result = await httpClient.GetByteArrayAsync($"https://github.com/games-on-whales/gow/blob/master/apps/{name}/assets/icon.png?raw=true");
            Image image = new();
            image.LoadPngFromBuffer(result);
            Texture2D texture2D = ImageTexture.CreateFromImage(image);
            return texture2D;
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
            var result = await _httpClient.GetStringAsync("http://localhost/api/v1/profiles");
            Profiles profiles = JsonSerializer.Deserialize<Profiles>(result);

            if(!profiles.success)
            {
                GD.Print("Error retrieving Profiles");
                return [];
            }

            foreach(var profile in profiles.profiles)
            {
                if(profile.id == used_profile.id)
                    return profile.apps;
            }
            GD.Print($"Profile: {used_profile.name} not found");
            //Apps wolfApps = JsonSerializer.Deserialize<Apps>(result);
            //if(wolfApps?.success == true)
            //    return wolfApps.apps;
            return [];
        }

        public static async Task<List<Profile>> GetProfiles()
        {
            var result = await _httpClient.GetStringAsync("http://localhost/api/v1/profiles");
            Profiles profiles = JsonSerializer.Deserialize<Profiles>(result);

            if(!profiles.success)
            {
                GD.Print("Error retrieving Profiles");
                return [];
            }

            return profiles.profiles;
        }

        public static async Task<List<Client>> GetClients()
        {
            var result = await _httpClient.GetStringAsync("http://localhost/api/v1/clients");
            Clients wolfClients = JsonSerializer.Deserialize<Clients>(result);
            if(wolfClients?.success == true)
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
            string data = JsonSerializer.Serialize<Starter>(starter);
            StringContent content = new(data);
            var result = await _httpClient.PostAsync("http://localhost/api/v1/runners/start", content);
            GD.Print(await result.Content.ReadAsStringAsync());
        }

        public static async Task<List<Lobby>> GetLobbies()
        {
            Lobbies lobbies = await GetAsync<Lobbies>("http://localhost/api/v1/lobbies");
            if(lobbies?.success == true)
                return lobbies.lobbies ?? [];
            return [];
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
            var result = await PostAsync<Lobby>("http://localhost/api/v1/lobbies/create", lobby);
            var content = await result.Content.ReadAsStringAsync();
            GD.Print($"called lobbies/create: {content}");
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
            var json = JsonSerializer.Serialize<LobbyJoin>(lobbyobj);

            /*
            string json = $@"
            {{
                ""lobby_id"": ""{lobby_id}"",
                ""moonlight_session_id"": ""{session_id}""
            }}";
            */
            StringContent content = new(json);
            var result = await _httpClient.PostAsync("http://localhost/api/v1/lobbies/join", content);
            GD.Print($"called lobbies/join: {await result.Content.ReadAsStringAsync()}");
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
            GD.Print(await result.Content.ReadAsStringAsync());
        }

        public static async Task StopLobby(string lobby_id)
        {
            string json = @$"{{""lobby_id"": ""{lobby_id}""}}";

            StringContent content = new(json);
            var result = await _httpClient.PostAsync("http://localhost/api/v1/lobbies/stop", content);
            GD.Print(await result.Content.ReadAsStringAsync());
        }

        private static async Task<HttpResponseMessage> PostAsync<T>(string url, T obj)
        {
            string data = JsonSerializer.Serialize<T>(obj);
            StringContent content = new(data);
            var result = await _httpClient.PostAsync(url, content);
            //GD.Print(await result.Content.ReadAsStringAsync());
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
}