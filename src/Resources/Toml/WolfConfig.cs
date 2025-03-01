using Godot;
using System;
using System.Collections.Generic;
using Tomlyn;

namespace WolfManagement.Resources
{
    [GlobalClass]
    public partial class WolfConfig : Resource
    {
        [Export]
        String ConfigPath = "/etc/wolf/cfg/config.toml";

        private WolfTomlModel ConfigToml {
            get{
                return Toml.ToModel<WolfTomlModel>(FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read).GetAsText());
            }
            set{}
        }

        public List<WolfApp> GetApps()
        {
            return ConfigToml.Apps;
        }

        public List<WolfClient> GetUser()
        {
            return ConfigToml.Paired_clients;
        }

        public void Add(WolfApp app)
        {
            ConfigToml.Apps.Add(app);
            //TODO: SAVE!!!
        }
    }
}
