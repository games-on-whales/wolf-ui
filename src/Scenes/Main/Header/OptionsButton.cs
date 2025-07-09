using Godot;
using WolfUI;

public partial class OptionsButton : Button
{
    public override void _Ready()
    {
        Pressed += OnPressed;
    }

    private static void OnPressed()
    {
        if (Main.Singleton.UserList is UserList node) node.Visible = true;
    }

    public override void _ExitTree()
    {
        Pressed -= OnPressed;
    }
}
