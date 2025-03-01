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
    }
}
