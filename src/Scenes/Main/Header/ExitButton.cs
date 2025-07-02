using Godot;
using Resources.WolfAPI;
using System;


public partial class ExitButton : Button
{
    public override void _Ready()
    {
        Pressed += () =>
        {
            Engine.PrintErrorMessages = false;

		    string auto_update_env = System.Environment.GetEnvironmentVariable("WOLF_UI_AUTOUPDATE");
		    bool AutoupdateEnable = auto_update_env is null || auto_update_env == "True";
            if (AutoupdateEnable)
            {
                WolfAPI.StopSession(WolfAPI.session_id);
            }

            GetTree().Quit();
        };
    }
}
