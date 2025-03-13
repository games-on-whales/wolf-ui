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
        public Runner runner {get;set;}
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
        public Runner runner {get;set;}
    }

    public class Runner
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

    public class Profiles
    {
        public bool success {get;set;}
        public List<Profile> profiles {get;set;}
    }

    public class Profile
    {
        public string id {get;set;}
        public string name {get;set;}
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string icon_png_path {get;set;}
        public List<App> apps {get;set;}
    }

/*
struct CreateLobbyEvent {
  const std::string name;
  const bool multi_user;
  const bool stop_when_everyone_leaves;

  struct VideoSettings {
    int width;
    int height;
    int refresh_rate;
    std::string wayland_render_node;
    std::string runner_render_node;
  } const video_settings;

  struct AudioSettings {
    int channel_count;
  } const audio_settings;

  const config::ClientSettings client_settings = {};
  const std::string runner_state_folder;
  /**
   * The app that will be run in the lobby
   *//*
  std::shared_ptr<Runner> runner;
};
*/
    public class Lobby
    {
        public string name;
        public bool multi_user;
        public bool stop_when_everyone_leaves;
        public string runner_state_folder;
        public Runner runner;
    }
}