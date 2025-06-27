using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Godot;

namespace Resources.WolfAPI;

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

public class App : EventArgs
{
    public string icon_png_path {get;set;}
    public bool start_virtual_compositor {get;set;}
    public string title {get;set;}
    public string id {get;set;}
    public Runner runner {get;set;}
    public string render_node {get;set;}
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

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<int> pin { get; set; }
    public List<App> apps { get; set; }
}

public class AudioSettings
{
    public int channel_count {get;set;}
}

public class VideoSettings
{
    public int width { get; set; }
    public int height { get; set; }
    public int refresh_rate { get; set; }
    public string wayland_render_node { get; set; }
    public string runner_render_node { get; set; }
    public string video_producer_buffer_caps { get; set; }
}

public class Lobbies
{
    public bool success {get;set;}
    public List<Lobby> lobbies {get;set;}
}

public class LobbyCreatedResponse
{
    public bool success {get;set;}
    public string lobby_id {get;set;}
}

public class LobbyJoin
{
    public string lobby_id { get; set; }
    public string moonlight_session_id { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<int> pin { get; set; }
}

public class Lobby : EventArgs
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string profile_id { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string started_by_profile_id { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string id { get; set; }
    public string name { get; set; }
    public bool pin_required { get; set; }
    public List<int> pin { get; set; }
    public bool multi_user { get; set; }
    public bool stop_when_everyone_leaves { get; set; }
    public string runner_state_folder { get; set; }
    public Runner runner { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ClientSettings client_settings { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VideoSettings video_settings { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AudioSettings audio_settings { get; set; }
}

public class Session
{
    public string app_id {get;set;}
    public string client_id {get;set;}
    public string client_ip {get;set;}
    public int video_width {get;set;}
    public int video_height {get;set;}
    public int video_refresh_rate {get;set;}
    public int audio_channel_count {get;set;}
    public ClientSettings client_settings {get;set;}
}

public class Sessions
{
    public bool success {get;set;}
    public List<Session> sessions {get;set;}
}