using Godot;
using System;
using UI;

[GlobalClass]
public partial class SoundEffects : Node
{
	[Export]
	AudioStream AcceptSound;
	[Export]
	AudioStream HoverSound;

	private AudioStreamPlayer2D HoverSoundPlayer;
	private AudioStreamPlayer2D AcceptSoundPlayer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		HoverSoundPlayer = new(){ Stream = HoverSound };
		AcceptSoundPlayer= new(){ Stream = AcceptSound };
		AddChild(HoverSoundPlayer);
		AddChild(AcceptSoundPlayer);

		var Main = GetNode<Main>("/root/Main");
		CallDeferred(MethodName.ApplySoundEffects, Main);
	}

	public void ApplySoundEffects(Node parent)
	{
		foreach(var child in parent.GetChildren())
		{
			if(child is Button button)
			{
				GD.Print($"Applying to {button.Name}");
				button.FocusEntered += () => { HoverSoundPlayer.Play(); };
				button.Pressed += () => { AcceptSoundPlayer.Play(); };
			}
			ApplySoundEffects(child);
		}
	}
}
