using Godot;
using System;
using WolfManagement.Resources;

public partial class ConfigureApp : Control
{
	[Export]
	WolfConfig config;
	[Export]
	LineEdit TitleEdit;
	[Export]
	CheckButton VirtualCompositor;
	[Export]
	Button ImageButton;
	[Export]
	TextEdit Base_create_json_Edit;
	[Export]
	StringArrayContainer Devices;
	[Export]
	StringArrayContainer Env;
	[Export]
	StringArrayContainer Mounts;
	[Export]
	StringArrayContainer Ports;
	[Export]
	LineEdit ImageLineEdit;
	[Export]
	LineEdit NameLineEdit;
	[Export]
	LineEdit TypeLineEdit;

	public WolfApp WolfApp;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		WolfApp ??= new(){
			Title = "",
			Start_virtual_compositor = true,
			Icon_png_path = "",
			Runner = new(){
				Base_create_json = @"{
	""HostConfig"": {
		""IpcMode"": ""host"",
		""CapAdd"": [""NET_RAW"", ""MKNOD"", ""NET_ADMIN"", ""SYS_ADMIN"", ""SYS_NICE""],
		""Privileged"": false,
		""DeviceCgroupRules"": [""c 13:* rmw"", ""c 244:* rmw""]
	}
}",
				Devices = new(),
				Env = new(){
					"RUN_SWAY=1", "GOW_REQUIRED_DEVICES=/dev/input/event* /dev/dri/* /dev/nvidia*"
				},
				Image = "",
				Mounts = new(),
				Name = "",
				Ports = new(),
				Type = "docker"
			}
		};

		TitleEdit.Text = WolfApp.Title;
		VirtualCompositor.ButtonPressed = WolfApp.Start_virtual_compositor;
		if(WolfApp.Icon_png_path != "")
		{
			Image image = Image.LoadFromFile(WolfApp.Icon_png_path);
			Texture2D texture = ImageTexture.CreateFromImage(image);

			ImageButton.Icon = texture;
		}
		Base_create_json_Edit.Text = WolfApp.Runner.Base_create_json;
		foreach(var device in WolfApp.Runner.Devices)
		{
			Devices.AddEntry(device);
		}
		foreach(var env in WolfApp.Runner.Env)
		{
			Env.AddEntry(env);
		}
		foreach(var mount in WolfApp.Runner.Mounts)
		{
			Mounts.AddEntry(mount);
		}
		foreach(var port in WolfApp.Runner.Ports)
		{
			Ports.AddEntry(port);
		}
		ImageLineEdit.Text = WolfApp.Runner.Image;
		TypeLineEdit.Text = WolfApp.Runner.Type;
		NameLineEdit.Text = WolfApp.Runner.Name;

		TitleEdit.TextChanged += (value) => {
			WolfApp.Title = value;
		};
		VirtualCompositor.Pressed += () => {
			WolfApp.Start_virtual_compositor = VirtualCompositor.ButtonPressed;
		};
		Base_create_json_Edit.TextChanged += () => {
			WolfApp.Runner.Base_create_json = Base_create_json_Edit.Text;
        };
	}

	public void AddToConfig()
	{
		config.Add(WolfApp);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
