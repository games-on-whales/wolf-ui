using Godot;
using System;

public partial class OptionsButton : Button
{
    public override void _Ready()
    {
        Pressed += () => GetNode<Control>("%UserList").Visible = true;
    }
}
