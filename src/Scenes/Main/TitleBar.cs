using Godot;
using System;

public partial class TitleBar : MarginContainer
{
	[Export]
	Button ExitButton;
	[Export]
	Button UserButton;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ExitButton.Pressed += ExitApplication;
		SetProcess(false);
	}

	private void ExitApplication()
	{
		GetTree().Quit();
	}
}
