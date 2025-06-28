using Godot;
using System;

namespace WolfUI;

[Tool]
[GlobalClass]
public partial class ThemedSVG : TextureRect
{
    public override void _Ready()
    {
        SetIconColor();
        ThemeChanged += SetIconColor;
    }

    private void SetIconColor()
    {
        var color = GetThemeColor("font_color");
        SelfModulate = color;
    }
}
