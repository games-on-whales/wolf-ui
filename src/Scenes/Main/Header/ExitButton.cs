using Godot;
using System;

public partial class ExitButton : Button
{
    public override void _Ready()
    {
        Pressed += () => GetTree().Quit();
    }
}
