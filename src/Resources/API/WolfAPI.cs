
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WolfManagement.Resources;

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
    public static async Task<List<WolfApp>> GetApps()
    {
        var result = await _httpClient.GetStringAsync("http://localhost/api/v1/apps");

        WolfApps wolfApps = JsonSerializer.Deserialize<WolfApps>(result);
        GD.Print(wolfApps);
        if(wolfApps?.success == true)
            return wolfApps.apps;
        return [];
    }
    public static async Task<List<WolfClient>> GetClients()
    {
        var result = await _httpClient.GetStringAsync("http://localhost/api/v1/clients");
        WolfClients wolfClients = JsonSerializer.Deserialize<WolfClients>(result);
        if(wolfClients?.success == true)
            return wolfClients.clients;
        return [];
    }

    public static async Task StartApp(WolfAppRunner runner, bool joinable = false)
    {
        var starter = new WolfStarter()
        {
            stop_stream_when_over = false,
            session_id = session_id,
            runner = runner
        };
        string data = JsonSerializer.Serialize<WolfStarter>(starter);
        StringContent content = new(data);
        var result = await _httpClient.PostAsync("http://localhost/api/v1/runners/start", content);
        GD.Print(await result.Content.ReadAsStringAsync());
    }

    public static async Task JoinCoopSession(string parent_session_id)
    {
        var starter = new WolfStarter()
        {
            stop_stream_when_over = false,
            session_id = session_id,
            runner = new(){
                type = "child_session",
                parent_session_id = parent_session_id
            }
        };

        string data = JsonSerializer.Serialize<WolfStarter>(starter);
        StringContent content = new(data);
        var result = await _httpClient.PostAsync("http://localhost/api/v1/runners/start", content);
        GD.Print(await result.Content.ReadAsStringAsync());
    }

	[Signal]
	public delegate void APIEventEventHandler(string eventType, string data);
}