using Godot;
using System;

[GlobalClass]
public partial class StringArrayContainer : Control
{
	[Export]
	Button AddEntryButton;
	[Export]
	PackedScene ContainerEntryScene;
	[Export]
	VBoxContainer EntryContainer;
	[Export]
	Label NameLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AddEntryButton.Pressed += () => {
			AddEntry("");
		};
		NameLabel.Text = Name;
	}

	public void AddEntry(string value)
	{
		var Entry = ContainerEntryScene.Instantiate();
		if(Entry is StringArrayContainerEntry e)
		{
			e.SetText(value);
		}
		EntryContainer.AddChild(Entry);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
