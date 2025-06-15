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

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			SoundEffects soundEffects = null;
			foreach (var child in GetChildren())
			{
				if (child is SoundEffects effects)
					soundEffects = effects;
			}

			soundEffects.CallDeferred(SoundEffects.MethodName.ApplySoundEffects, this);

			var time = new Timer
			{
				WaitTime = 0.1,
				OneShot = false,
				Autostart = true
			};
			time.Timeout += () =>
			{
				soundEffects.ApplySoundEffects(this);
			};

			AddChild(time);

			docker.UpdateCachedImages();
			var time2 = new Timer
			{
				WaitTime = 10.0,
				OneShot = false,
				Autostart = true
			};
			time2.Timeout += () =>
			{
				docker.UpdateCachedImages();
			};

			AddChild(time2);

			wolfAPI.StartListenToAPIEvents();

			GD.Print($"This session's id: {WolfAPI.session_id}");
		}

		public override void _Input(InputEvent @event)
		{
			controllerMap.SetController(@event);
		}
	}
}