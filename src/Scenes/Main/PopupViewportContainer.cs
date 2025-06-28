using Godot;
using System;

namespace WolfUI;

public partial class PopupViewportContainer : SubViewportContainer
{
    public override void _Ready()
    {
        var TopLayer = Main.Singleton.TopLayer;

        TopLayer.ChildExitingTree += (child) =>
        {
            if (TopLayer.GetChildCount() - 2 <= 0)
                Visible = false;
        };

        TopLayer.ChildEnteredTree += (child) =>
        {
            Visible = true;
        };

        Hide();
    }
}
