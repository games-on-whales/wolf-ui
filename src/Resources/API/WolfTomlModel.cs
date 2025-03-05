using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

namespace WolfManagement.Resources
{
    public class WolfTomlModel
    {
        public int Config_version {get;set;}
        public string Hostname {get;set;}
        public string Uuid {get;set;}
        public WolfGstreamer Gstreamer {get;set;}
        public List<WolfApp> Apps {get;set;}
        public List<WolfClient> Paired_clients {get;set;}
    }

    public class WolfGstreamer
    {
        public WolfGstreamerAudio Audio {get;set;}
        public WolfGstreamerVideo Video {get;set;}
    }

    public class WolfGstreamerAudio
    {
        public string Default_audio_params {get;set;}
        public string Default_opus_encoder {get;set;}
        public string Default_sink {get;set;}
        public string Default_source {get;set;}
    }

    public class WolfGstreamerVideo
    {
        public string Default_sink {get;set;}
        public string Default_source {get;set;}
        public WolfGstreamerVideoDefaults Defaults {get;set;}
        public List<WolfGstreamerVideoEncoder> Av1_encoders {get;set;}
        public List<WolfGstreamerVideoEncoder> H264_encoders {get;set;}
        public List<WolfGstreamerVideoEncoder> Hevc_encoders {get;set;}
    }

    public class WolfGstreamerVideoDefaults
    {
        public WolfGstreamerVideoDefaultsEntry Nvcodec {get;set;}
        public WolfGstreamerVideoDefaultsEntry Qsv {get;set;}
        public WolfGstreamerVideoDefaultsEntry Vaapi {get;set;}
    }

    public class WolfGstreamerVideoEncoder
    {
        public List<string> Check_elements {get;set;}
        public string Encoder_pipeline {get;set;}
        public string Plugin_name {get;set;}
        public string Video_params {get;set;}
    }

    public class WolfGstreamerVideoDefaultsEntry
    {
        public string Video_params {get;set;}
    }

    public class WolfApp
    {
        public string Raw_Json {get;set;}
        public string Icon_png_path {get;set;}
        public bool Start_virtual_compositor {get;set;}
        public string Title {get;set;}
        public WolfAppRunner Runner {get;set;}
    }

    public class WolfAppRunner
    {
        public string Base_create_json {get;set;}
        public List<string> Devices {get;set;}
        public List<string> Env {get;set;}
        public string Image {get;set;}
        public List<string> Mounts {get;set;}
        public string Name {get;set;}
        public List<string> Ports {get;set;}
        public string Type {get;set;}
        public string ToJson()
        {
            string MountsString = "";
            foreach(var s in Mounts)
            {
                MountsString += $"\"{s}\", ";
            }
            MountsString = MountsString.Length > 0 ? MountsString.TrimSuffix(", ") : "";
            string EnvString = "";
            foreach(var s in Env)
            {
                EnvString += $"\"{s}\", ";
            }
            EnvString = EnvString.Length > 0 ? EnvString.TrimSuffix(", ") : "";
            string DevicesString = "";
            foreach(var s in Devices)
            {
                DevicesString += $"\"{s}\", ";
            }
            DevicesString = DevicesString.Length > 0 ? DevicesString.TrimSuffix(", ") : "";
            string PortsString = "";
            foreach(var s in Ports)
            {
                PortsString += $"\"{s}\", ";
            }
            PortsString = PortsString.Length > 0 ? PortsString.TrimSuffix(", ") : "";
            return $@"{{ ""type"":""{Type}"", ""name"":""{Name}"", ""image"":""{Image}"", ""mounts"": [{MountsString}], ""env"": [{EnvString}], ""devices"": [{DevicesString}], ""ports"": [{PortsString}], ""base_create_json"": ""{Base_create_json.Replace("\"", "\\\"").Replace("\n", "")}""}}";
        }
    }

    public class WolfClient
    {
        public string Client_id {get;set;}
        public string App_state_folder {get;set;}
        public string Client_cert {get;set;}
        public WolfClientSettings Settings {get;set;}
    }

    public class WolfClientSettings
    {
        public List<string> Controllers_override {get;set;}
        public float H_scroll_acceleration {get;set;}
        public float Mouse_acceleration {get;set;}
        public int Run_gid {get;set;}
        public int Run_uid {get;set;}
        public float V_scroll_acceleration {get;set;}
    }
}