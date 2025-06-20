using Godot;
using WolfManagement.Resources;
using Resources.WolfAPI;
using System.Threading.Tasks;
using System.Linq;
using System.Data.Common;

namespace UI
{
	[GlobalClass]
	public partial class Main : Control
	{
		[Export]
		public DockerController docker;
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

			if (!DockerController.isDisabled)
			{
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
			}

			WolfAPI.Init();

			CheckForUpdate();

			GD.Print($"This session's id: {WolfAPI.session_id}");
		}

		public override void _Input(InputEvent @event)
		{
			controllerMap.SetController(@event);
		}

		private async void CheckForUpdate()
		{
			var apps = await WolfAPI.GetAsync<Apps>("http://localhost/api/v1/apps");

			App Wolf_UI = apps.apps.FindAll(app => app.runner.image is not null
											&& app.runner.image.Contains("wolf-ui")
											&& app.runner.env.Contains("WOLF_UI_AUTOUPDATE=True"))
									.FirstOrDefault();

			if (Wolf_UI is not null)
			{
				string auto_update_env = System.Environment.GetEnvironmentVariable("WOLF_UI_AUTOUPDATE");
				bool AutoupdateEnable = auto_update_env is null || auto_update_env == "True";
				if (AutoupdateEnable)
				{
					if (Wolf_UI.runner.image.Contains(':'))
					{
						var image = Wolf_UI.runner.image.Split(":");
						await docker.PullImage(image[0], image[1], null, null);
					}
				}
			}
		}
	}
}