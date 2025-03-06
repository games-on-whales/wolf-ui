using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Godot;

namespace Resources.WolfAPI
{
    public class Starter
    {
        public bool stop_stream_when_over {get;set;}
        public string session_id {get;set;}
        public AppRunner runner {get;set;}
    }

    public class Apps
    {
        public bool success {get;set;}
        public List<App> apps {get;set;}
    }

    public class App
    {
        public string icon_png_path {get;set;}
        public bool start_virtual_compositor {get;set;}
        public string title {get;set;}
        public AppRunner runner {get;set;}
    }

    public class AppRunner
    {
        public string type {get;set;}

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string base_create_json {get;set;}

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> devices {get;set;}

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> env {get;set;}

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string image {get;set;}

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> mounts {get;set;}

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string name {get;set;}

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> ports {get;set;}

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string parent_session_id {get;set;}
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string run_cmd {get;set;}
    }

    public class Clients
    {
        public bool success {get;set;}
        public List<Client> clients {get;set;}
    }

    public class Client
    {
        public string client_id {get;set;}
        public string app_state_folder {get;set;}
        public string client_cert {get;set;}
        public ClientSettings settings {get;set;}
    }

    public class ClientSettings
    {
        public List<string> controllers_override {get;set;}
        public float h_scroll_acceleration {get;set;}
        public float mouse_acceleration {get;set;}
        public int run_gid {get;set;}
        public int run_uid {get;set;}
        public float v_scroll_acceleration {get;set;}
    }
}