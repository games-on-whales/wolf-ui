using Godot;
using System;

namespace WolfUI;

[Tool]
public partial class Settings : Control
{
    [Export]
    bool SwitchSzene
    {
        get
        {
            return false;
        }
        set
        {
            ProfileSettings.Visible = !ProfileSettings.Visible;
            AppSettings.Visible = !AppSettings.Visible;
        }
    }

    Control ProfileSettings;
    Control AppSettings;

    public override void _Ready()
    {
        base._Ready();
        ProfileSettings = GetNode<Control>("%ProfileSettings");
        AppSettings = GetNode<Control>("%AppSettings");

        if (Engine.IsEditorHint())
        {
            return;
        }
    }
}
