
using Godot;
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

    private static string session_id = "";
    private static bool StartedListening = false;

    public WolfAPI()
    {
        session_id = System.Environment.GetEnvironmentVariable("WOLF_SESSION_ID");
		if(session_id == null)
		{
			GD.Print("session_id not found!");
            session_id = "123456789";
		}

        APIEvent += FilterAPIEvents;
    }

    [Signal]
    public delegate void StartRunnerEventHandler(string data);

    private void FilterAPIEvents(string @event, string data)
    {
        var error = @event switch
        {
            "wolf::core::events::StartRunner" => EmitSignal(SignalName.StartRunner, data),
            "wolf::core::events::StreamSession" => Error.DoesNotExist,
            "wolf::core::events::StopStreamEvent" => Error.DoesNotExist,
            "wolf::core::events::VideoSession" => Error.DoesNotExist,
            "wolf::core::events::RTPAudioPingEvent" => Error.DoesNotExist,
            "wolf::core::events::AudioSession" => Error.DoesNotExist,
            "wolf::core::events::IDRRequestEvent" => Error.DoesNotExist,
            "wolf::core::events::RTPVideoPingEvent" => Error.DoesNotExist,
            _ => Error.Unconfigured,
        };
        if(error == Error.Unconfigured)
        {
            GD.Print(@event);
            GD.Print(data);
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

    public static async Task JoinCoopSession(string parent_session_id)
    {
        var starter = new Starter()
        {
            stop_stream_when_over = false,
            session_id = session_id,
            runner = new(){
                type = "child_session",
                parent_session_id = parent_session_id
            }
        };

        string data = JsonSerializer.Serialize<Starter>(starter);
        StringContent content = new(data);
        var result = await _httpClient.PostAsync("http://localhost/api/v1/runners/start", content);
        GD.Print(await result.Content.ReadAsStringAsync());
    }

	[Signal]
	private delegate void APIEventEventHandler(string eventType, string data);
}
}