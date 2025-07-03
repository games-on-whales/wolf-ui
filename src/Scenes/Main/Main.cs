using Godot;
using Resources.WolfAPI;
using System.Linq;
//using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace WolfUI;

[GlobalClass]
public partial class Main : Control
{
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

	private SubViewport _TopLayer;
	public SubViewport TopLayer
	{
		get
		{
			_TopLayer ??= GetNode<SubViewport>("%TopLayer");
			return _TopLayer;
		}
	}

	static Main()
	{
		var str_to_enum = new Dictionary<string, ILoggerFactory.LogLevelEnum>
		{
			{ "NONE", ILoggerFactory.LogLevelEnum.NONE},
			{ "ERROR", ILoggerFactory.LogLevelEnum.ERROR},
			{ "WARNING", ILoggerFactory.LogLevelEnum.WARN},
			{ "WARN", ILoggerFactory.LogLevelEnum.WARN},
			{ "INFORMATION", ILoggerFactory.LogLevelEnum.INFO},
			{ "INFO", ILoggerFactory.LogLevelEnum.INFO},
			{ "DEBUG", ILoggerFactory.LogLevelEnum.DEBUG}
		};
		var log_level_env = System.Environment.GetEnvironmentVariable("LOGLEVEL") ?? "INFO";
        if (!str_to_enum.TryGetValue(log_level_env.ToUpper(), out var logLevel))
        {
            logLevel = ILoggerFactory.LogLevelEnum.INFO;
        }

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
		factory.SetLogLevel(logLevel);
		Logger = factory.CreateLogger<Main>();
		Logger.LogInformation("Wolf-UI started.");
	}

	public static ILogger<T> GetLogger<T>()
	{
		return factory.CreateLogger<T>();
	}

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		if (Engine.IsEditorHint())
			return;

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

		WolfAPI.Init();

		string auto_update_env = System.Environment.GetEnvironmentVariable("WOLF_UI_AUTOUPDATE");
		bool AutoupdateEnable = auto_update_env is null || auto_update_env == "True";
		if (AutoupdateEnable)
		{
			var apps = await WolfAPI.GetApps();
			App Wolf_UI = apps?.apps.FindAll(app => app.runner.image is not null
											&& app.runner.image.Contains("wolf-ui")
											&& app.runner.env.Contains("WOLF_UI_AUTOUPDATE=True"))
									.FirstOrDefault();

			WolfAPI.Singleton.ImageUpdated += async (img) =>
			{
				if (img == Wolf_UI.runner.image)
				{
					if (await QuestionDialogue.OpenDialogue<bool>("Restart", "Wolf-UI has been updated, please restart.",
						new Dictionary<string, bool> {
							{ "Restart", true },
							{ "Later", false }
						}))
					{
						await WolfAPI.StartApp(Wolf_UI.runner);
						await Task.Delay(500);
						GetTree().Quit();
					}
				}
			};
			if(Wolf_UI is not null)
				WolfAPI.PullImage(Wolf_UI.runner.image);
		}

		Logger.LogInformation("This session's id: {0}", WolfAPI.session_id);

	}

	public override void _EnterTree()
	{
		base._EnterTree();
		//GetTree().Root.Theme = ResourceLoader.Load<Theme>("uid://v418qqxvwy87");
    }

	public void LoadTheme(string theme_name)
	{
		string user = System.Environment.GetEnvironmentVariable("USER");
		user = (user == "root" || user == null) ? "retro" : user;
		string filepath = $"/home/{user}/.wolf-ui/{theme_name}.tres";

		if (File.Exists(filepath))
		{
			return;
		}

		GetTree().Root.Theme = ResourceLoader.Load<Theme>(filepath);
	}

	public void SaveTheme(string theme_name)
	{
		string user = System.Environment.GetEnvironmentVariable("USER");
		user = (user == "root" || user == null) ? "retro" : user;
		string filepath = $"/home/{user}/.wolf-ui/{theme_name}.tres";

		if (File.Exists(filepath))
		{
			return;
		}

		//GetTree().Root.Theme;
	}

	public override void _Input(InputEvent @event)
	{
		controllerMap.SetController(@event);
	}
}
