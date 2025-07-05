using Godot;

namespace WolfUI;

[GlobalClass]
public partial class ControllerIcon : TextureRect
{
#nullable disable
    [Export]
    ControllerMap controllerMap;
    [Export]
    ControllerMap.ControllerButton buttonIcon;
#nullable enable
    public override void _Ready()
    {
        controllerMap.IconSetChanged += () => {
            Texture = controllerMap.GetIcon(buttonIcon);
        };
    }
}
