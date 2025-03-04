
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

    private static bool StartedListening = false;
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

    public async Task Testcall(string url)
    {
        var result = await _httpClient.GetStringAsync(url);
        GD.Print(result);
    }

    public async Task<List<WolfClient>> GetClients()
    {
        var result = await _httpClient.GetStringAsync("http://localhost/api/v1/clients");
        var json = (Dictionary)Json.ParseString(result);
        
        if(!(bool)json["success"])
            return new();

        List<WolfClient> clients = new();
        foreach(Dictionary json_client in (Godot.Collections.Array<Dictionary>)json["clients"])
        {
            WolfClient c =new(){
                Client_id = (string)json_client["client_id"],
                App_state_folder = (string)json_client["app_state_folder"],
                Settings = new(){
                    Controllers_override = new(),//(Array<string>)((Dictionary)json_client["settings"])["run_uid"],
                    Run_uid = (int)((Dictionary)json_client["settings"])["run_uid"],
                    Run_gid = (int)((Dictionary)json_client["settings"])["run_gid"],
                    Mouse_acceleration = (float)((Dictionary)json_client["settings"])["mouse_acceleration"],
                    V_scroll_acceleration = (float)((Dictionary)json_client["settings"])["v_scroll_acceleration"],
                    H_scroll_acceleration = (float)((Dictionary)json_client["settings"])["h_scroll_acceleration"]
                }
            };
            foreach(string controller in (Array<string>)((Dictionary)json_client["settings"])["controllers_override"])
            {
                c.Settings.Controllers_override.Add(controller);
            }
            clients.Add(c);
        }
        return clients;
    }

    public async Task StartApp(WolfApp app, bool stop_stream_when_over, string session_id)
    {
        string runner = app.Runner.ToJson();
        string data = $@"{{
                            ""stop_stream_when_over"": {(stop_stream_when_over ? "true" : "false")}, 
                            ""session_id"": ""{session_id}"", 
                            ""runner"": {runner} 
                        }}";
        StringContent content = new(data.ToString());
        //GD.Print(data);

        var result = await _httpClient.PostAsync("http://localhost/api/v1/runners/start", content);
        GD.Print();
        GD.Print(result.StatusCode);
        GD.Print(await result.Content.ReadAsStringAsync());
    }

    public async Task<List<WolfApp>> GetApps()
    {
        var result = await _httpClient.GetStringAsync("http://localhost/api/v1/apps");
        //GD.Print(result);
        var json = (Dictionary)Json.ParseString(result);

        if(!(bool)json["success"])
            return new();

        List<WolfApp> apps = new();
        foreach(Dictionary json_app in (Godot.Collections.Array<Dictionary>)json["apps"])
        {
            var app = new WolfApp(){
                Icon_png_path = (string)json_app.GetValueOrDefault("icon_png_path", ""),
                Title = (string)json_app["title"],
                Runner = new(){
                    Type = (string)((Dictionary)json_app["runner"])["type"],
                    Name = (string)((Dictionary)json_app["runner"])["name"],
                    Image= (string)((Dictionary)json_app["runner"])["image"],
                    Base_create_json=(string)((Dictionary)json_app["runner"])["base_create_json"],
                    Devices = new(),
                    Env = new(),
                    Mounts = new(),
                    Ports = new()
                }
            };
            foreach(string device in (Array<string>)((Dictionary)json_app["runner"])["devices"])
            {
                app.Runner.Devices.Add(device);
            }
            foreach(string env in (Array<string>)((Dictionary)json_app["runner"])["env"])
            {
                app.Runner.Env.Add(env);
            }
            foreach(string mount in (Array<string>)((Dictionary)json_app["runner"])["mounts"])
            {
                app.Runner.Mounts.Add(mount);
            }
            foreach(string port in (Array<string>)((Dictionary)json_app["runner"])["ports"])
            {
                app.Runner.Ports.Add(port);
            }

            apps.Add(app);
        }

        return apps;
    }

	[Signal]
	public delegate void APIEventEventHandler(string eventType, string data);
}