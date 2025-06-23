using Godot;
using WolfManagement.Resources;
using Resources.WolfAPI;
using System.Linq;
//using Microsoft.Extensions.Logging;
using System;
using System.Globalization;

namespace WolfUI;

[GlobalClass]
public partial class Main : Control
{
	[Export]
	public DockerController docker;
	[Export]
	public ControllerMap controllerMap;
	private readonly static ILoggerFactory factory;
	private static readonly ILogger<Main> Logger;

	private static Main _Singleton;
	public static Main Singleton
	{
		get
		{
			return _Singleton;
		}
	}

	public Main()
	{
		_Singleton ??= this;
	}

	private Node _TopLayer;
	public Node TopLayer
	{
		get
		{
			_TopLayer ??= GetNode<Node>("%TopLayer");
			return _TopLayer;
		}
	}

	static Main()
	{
		factory = LoggerFactory.Create();
		/*
			builder => builder.AddSimpleConsole(options =>
			{
				options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
				options.IncludeScopes = true;
				options.SingleLine = true;
				options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
			}));
		*/
		Logger = factory.CreateLogger<Main>();
		Logger.LogInformation("Wolf-UI started.");
	}

	public static ILogger<T> GetLogger<T>()
	{
		return factory.CreateLogger<T>();
	}

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

		Logger.LogInformation("This session's id: {0}", WolfAPI.session_id);
		//GD.Print($"This session's id: {WolfAPI.session_id}");
	}

	public override void _Input(InputEvent @event)
	{
		controllerMap.SetController(@event);
	}

	private async void CheckForUpdate()
	{
		if (DockerController.isDisabled)
			return;

		string auto_update_env = System.Environment.GetEnvironmentVariable("WOLF_UI_AUTOUPDATE");
		bool AutoupdateEnable = auto_update_env is null || auto_update_env == "True";
		if (!AutoupdateEnable)
			return;

		var apps = await WolfAPI.GetAsync<Apps>("http://localhost/api/v1/apps");

		App Wolf_UI = apps.apps.FindAll(app => app.runner.image is not null
										&& app.runner.image.Contains("wolf-ui")
										&& app.runner.env.Contains("WOLF_UI_AUTOUPDATE=True"))
								.FirstOrDefault();

		if (Wolf_UI is not null && Wolf_UI.runner.image.Contains(':'))
		{
			var image = Wolf_UI.runner.image.Split(":");
			await docker.PullImage(image[0], image[1], null, null);
		}

		/* use after checking if a new version was pulled.
		bool shouldRestart = await QuestionDialogue.OpenDialogue<bool>(this, "Restart", "Wolf-UI has been updated, please restart.",
			new Dictionary<string, bool> {
				{ "Restart", true },
				{ "Later", false }
			});
		*/
	}
}
