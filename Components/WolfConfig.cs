using Godot;
using System;
using System.Collections.Generic;
using Tomlyn;
using Tomlyn.Model;

[GlobalClass]
public partial class WolfConfig : Resource
{
    [Export]
    String ConfigPath = "/etc/wolf/cfg/config.toml";

    private WolfTomlModel ConfigToml {
        get{
            return Toml.ToModel<WolfTomlModel>(FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read).GetAsText());
        }
        set{

        }
    }

    public void GetApps()
    {
        foreach(var app in ConfigToml.Apps)
        {
            GD.Print(app.Title);
        }
        
    }
}
