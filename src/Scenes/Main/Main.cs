using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WolfManagement.Resources;

namespace UI
{
	[GlobalClass]
	public partial class Main : Control
	{
		[Export]
		public DockerController docker;
		[Export]
		public WolfAPI wolfAPI;
		public WolfClient SelectedClient = null;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			SoundEffects soundEffects = null;
			foreach(var child in GetChildren())
			{
				if(child is SoundEffects effects)
					soundEffects = effects;
			}

			soundEffects.CallDeferred(SoundEffects.MethodName.ApplySoundEffects, this);

			var time = new Timer();
			time.WaitTime = 0.2;
			time.OneShot = false;
			time.Autostart = true;
			time.Timeout += () => {
				soundEffects.ApplySoundEffects(this);
			};

			AddChild(time);
		}
	}
}