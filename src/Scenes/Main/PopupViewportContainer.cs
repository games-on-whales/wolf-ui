using Godot;
using System;

namespace WolfUI;

public partial class PopupViewportContainer : SubViewportContainer
{
    public override void _Ready()
    {
        Hide();
    }

    public override void _Process(double delta)
    {
        var TopLayer = Main.Singleton.TopLayer;
        if (TopLayer.GetChildCount() <= 0)
            Visible = false;
        if (TopLayer.GetChildCount() > 0)
            Visible = true;
    }
}
