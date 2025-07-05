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

		    bool AutoupdateEnable = (System.Environment.GetEnvironmentVariable("WOLF_UI_AUTOUPDATE") ?? "False") == "True";
            if (AutoupdateEnable)
            {
                WolfAPI.StopSession(WolfAPI.Session_id);
            }

            GetTree().Quit();
        };
    }
}
