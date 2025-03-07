using Godot;
using WolfManagement.Resources;
using Resources.WolfAPI;

namespace UI
{
	[GlobalClass]
	public partial class Main : Control
	{
		[Export]
		public DockerController docker;
		[Export]
		public WolfAPI wolfAPI;
		[Export]
		public ControllerMap controllerMap;
		public Profile SelectedProfile = null;

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
			time.WaitTime = 0.1;
			time.OneShot = false;
			time.Autostart = true;
			time.Timeout += () => {
				soundEffects.ApplySoundEffects(this);
			};

			AddChild(time);


			/*
			var width = System.Environment.GetEnvironmentVariable("GAMESCOPE_WIDTH");
			var height = System.Environment.GetEnvironmentVariable("GAMESCOPE_HEIGHT");
			if(width != null && height != null)
			{
				GetWindow().Position = new Vector2I(0,0);
				GetWindow().Size = new Vector2I(Int32.Parse(width), Int32.Parse(height));
			}
			*/
			//DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);

			wolfAPI.StartListenToAPIEvents();
		}

		public override void _Input(InputEvent @event)
		{
			controllerMap.SetController(@event);
		}
    }
}