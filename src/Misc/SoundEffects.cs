using Godot;
using System.Collections.Generic;


namespace WolfUI;

[GlobalClass]
public partial class SoundEffects : Node
{
#nullable disable
	[Export]
	AudioStream AcceptSound;
	[Export]
	AudioStream HoverSound;

	private AudioStreamPlayer2D HoverSoundPlayer;
	private AudioStreamPlayer2D AcceptSoundPlayer;
#nullable enable
	private HashSet<Button> hasSound = [];

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		hasSound = [];
		HoverSoundPlayer = new() { Stream = HoverSound };
		AcceptSoundPlayer = new() { Stream = AcceptSound };
		AddChild(HoverSoundPlayer);
		AddChild(AcceptSoundPlayer);
	}

	public void ApplySoundEffects(Node parent)
	{
		foreach (var child in parent.GetChildren())
		{
			if (child is Button button && !hasSound.Contains(button))
			{

				button.FocusEntered += () => { HoverSoundPlayer.Play(); };
				button.Pressed += () => { AcceptSoundPlayer.Play(); };
				hasSound.Add(button);
			}
			ApplySoundEffects(child);
		}
	}
}
