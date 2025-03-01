using Godot;
using System;

public partial class StringArrayContainerEntry : Control
{
	[Export]
	LineEdit Entry;
	[Export]
	Button DeleteEntry;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		DeleteEntry.Pressed += () => {
			QueueFree();
		};
	}

	public void SetText(string value)
	{
		Entry.Text = value;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
