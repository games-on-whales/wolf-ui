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

	private static AudioStreamPlayer2D _hoverSoundPlayer;
	private static AudioStreamPlayer2D _acceptSoundPlayer;
#nullable enable
	private HashSet<Button> _hasSound = [];

	private static int _lockedAudio;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_hasSound = [];
		_hoverSoundPlayer = new AudioStreamPlayer2D { Stream = HoverSound };
		_acceptSoundPlayer = new AudioStreamPlayer2D { Stream = AcceptSound };
		AddChild(_hoverSoundPlayer);
		AddChild(_acceptSoundPlayer);
	}

	public void ApplySoundEffects(Node parent)
	{
		foreach (var child in parent.GetChildren())
		{
			if (child is Button button && !_hasSound.Contains(button))
			{

				button.FocusEntered += PlayHoverSound;
				button.Pressed += PlayAcceptSound;
				_hasSound.Add(button);
			}
			ApplySoundEffects(child);
		}
	}

	public static void PlayHoverSound()
	{
		if (_lockedAudio > 0) return;
		_hoverSoundPlayer.Play();
		_lockedAudio = 4;
	}

	public static void PlayAcceptSound()
	{
		if (_lockedAudio > 0) return;
		_acceptSoundPlayer.Play();
		_lockedAudio = 2;
	}

	public override void _Process(double delta)
	{
		if (_lockedAudio > 0)
			_lockedAudio--;
	}
}
