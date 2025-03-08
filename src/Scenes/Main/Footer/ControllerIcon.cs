using Godot;
using System;

[GlobalClass]
public partial class ControllerIcon : TextureRect
{
    [Export]
    ControllerMap controllerMap;
    [Export]
    ControllerMap.ControllerButton buttonIcon;

    public override void _Ready()
    {
        controllerMap.IconSetChanged += () => {
            Texture = controllerMap.GetIcon(buttonIcon);
        };
    }
}
