using Godot;
using Resources.WolfAPI;
using System.IO;
using Skerga.GodotNodeUtilGenerator;
using System.Text;

namespace WolfUI;

[GlobalClass, SceneAutoConfigure(GenerateNewMethod = false)]
public partial class Main : Control
{
	[Export]
	public ControllerMap? controllerMap;
	public static Main Singleton { get; private set; }

	public Main() 
	{
		Singleton ??= this;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
			return;

		SoundEffects? soundEffects = null;
		foreach (var child in GetChildren())
		{
			if (child is SoundEffects effects)
				soundEffects = effects;
		}

		soundEffects?.CallDeferred(SoundEffects.MethodName.ApplySoundEffects, this);

		var time = new Timer
		{
			WaitTime = 0.1,
			OneShot = false,
			Autostart = true
		};
		time.Timeout += () =>
		{
			soundEffects?.ApplySoundEffects(this);
		};

		AddChild(time);

		WolfApi.Init();

		SelfUpdateAsync();

		Logger.LogInformation("This session's id: {0}", WolfApi.SessionId);
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		//GetTree().Root.Theme = ResourceLoader.Load<Theme>("uid://v418qqxvwy87");
    }

	public void LoadTheme(string themeName)
	{
		string user = System.Environment.GetEnvironmentVariable("USER") ?? "retro";
		user = user == "root" ? "retro" : user;
		string filepath = $"/home/{user}/.wolf-ui/{themeName}.tres";

		if (File.Exists(filepath))
		{
			return;
		}

		GetTree().Root.Theme = ResourceLoader.Load<Theme>(filepath);
	}

	public void SaveTheme(string themeName)
	{
		var user = System.Environment.GetEnvironmentVariable("USER") ?? "retro";
		user = user == "root" ? "retro" : user;
		var filepath = $"/home/{user}/.wolf-ui/{themeName}.tres";

		if (File.Exists(filepath))
		{
			return;
		}

		//GetTree().Root.Theme;
	}

	public override void _Input(InputEvent @event)
	{
		controllerMap?.SetController(@event);
	}
}
