using System;
using System.Collections.Generic;
using Godot;
using Resources.WolfAPI;
using Skerga.GodotNodeUtilGenerator;

namespace WolfUI;

[GlobalClass]
[Tool]
[SceneAutoConfigure]
public partial class AppEntry : MarginContainer
{
	private enum AppState
	{
		NOTONDISK = 0,
		DOWNLOADING,
		PLAYING,
		OK,
		NONE
	}
	private AppState _State = AppState.NONE;
	private AppState State
	{
		get
		{
			return _State;
		}
		set
		{
			if (_State != value)
			{
				_State = value;
				OnStateChanged();
			}
		}
	}
	private Resources.WolfAPI.Lobby? RunningLobby = null;
	private App App;
	private static readonly ILogger<WolfAPI> Logger = WolfUI.Main.GetLogger<WolfAPI>();
	private bool IsImageOnDisc = true;

	//private static readonly PackedScene SelfRef = ResourceLoader.Load<PackedScene>("uid://chspw2lt1qcuc");

#nullable disable
	private AppEntry() { }
#nullable enable

	public static AppEntry Create(App app)
	{
		AppEntry entry = SelfRef.Instantiate<AppEntry>();
		entry.App = app;
		return entry;
	}

	private bool WasInView = false;
	[Signal]
	private delegate void AppEnteredViewEventHandler();

	[Signal]
	private delegate void AppRunningEventHandler();

	[Signal]
	private delegate void AppStoppedEventHandler();

	private bool IsAlreadyRunning(Resources.WolfAPI.Lobby lobby)
	{
		//check if the App.Title is the same as the lobbies Name
		if (lobby.name == App.title)
			return true;
		//check if this app uses the same folder as the lobby 
		if (App?.runner?.name is null || lobby.runner_state_folder == $"profile-data/{WolfAPI.Profile.id}/{App.runner.name}")
			return true;

		return false;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			return;
		}

		AppName.Text = App.title;
		//DownloadIcon.Hide();
		//AppProgress.Hide();
		AppButton.Pressed += OnPressed;

		FocusEntered += AppMenu.Hide;
		MenuButtonCancle.Pressed += AppButton.GrabFocus; //Hides menu via the FocusEntered above
		MenuButtonUpdate.Pressed += PullImage;
		MenuButtonCoop.Pressed += OnCoopPressed;
		MenuButtonStop.Pressed += OnStopPressed;
		MenuButtonStart.Pressed += OnStartPressed;

		State = AppState.OK;

		var appList = Main.Singleton.GetNode<AppList>("%AppList");
		appList.LobbyCreatedEvent += OnLobbyCreatedEvent;
		appList.LobbyStoppedEvent += OnLobbyStoppedEvent;


		AppRunning += () =>
		{
			State = AppState.PLAYING;
		};

		AppStopped += () =>
		{
			State = IsImageOnDisc ? AppState.OK : AppState.NOTONDISK;
		};

		WolfAPI.Singleton.ImageUpdated += OnImageUpdated;
		WolfAPI.Singleton.ImageAlreadyUptoDate += OnImageUpdated;
		WolfAPI.Singleton.ImagePullProgress += OnImagePullProgress;

		AppEnteredView += async () =>
		{
			AppIcon.Texture = await WolfAPI.GetAppIcon(App);
		};
	}

	public override void _ExitTree()
	{
		base._ExitTree();

		if (Engine.IsEditorHint())
		{
			return;
		}

		var appList = Main.Singleton.GetNode<AppList>("%AppList");
		appList.LobbyCreatedEvent -= OnLobbyCreatedEvent;
		appList.LobbyStoppedEvent -= OnLobbyStoppedEvent;
	}

	private void OnLobbyCreatedEvent(object? caller, Resources.WolfAPI.Lobby lobby)
	{
		if (!IsInstanceValid(this))
			return;
		if (IsAlreadyRunning(lobby))
		{
			RunningLobby = lobby;
			EmitSignalAppRunning();
		}
	}

	private void OnLobbyStoppedEvent(object? caller, string lobby_id)
	{
		if (!IsInstanceValid(this))
			return;
		if (lobby_id == RunningLobby?.id)
		{
			RunningLobby = null;
			EmitSignalAppStopped();
		}
	}

	private void OnImageUpdated(string image)
	{
		if (!IsInstanceValid(this))
			return;
		if (App?.runner?.image is null || image != App.runner.image)
			return;

		State = RunningLobby is null ? AppState.OK : AppState.PLAYING;
	}

	private void OnImagePullProgress(string image, double progress)
	{
		if (App?.runner?.image is null || image != App.runner.image)
			return;

		if (!IsInstanceValid(ProgressBar) || !IsInstanceValid(AppButton) || !IsInstanceValid(DisabledIndicator))
			return;

		State = AppState.DOWNLOADING;

		if (!ProgressBar.Visible || !AppButton.Disabled || !DisabledIndicator.Visible)
		{
			ProgressBar.Visible = true;
			AppButton.Disabled = true;
			DisabledIndicator.Visible = true;
		}

		ProgressBar.Value = progress;
	}

	public override async void _Process(double delta)
	{
		base._Process(delta);

		if (Engine.IsEditorHint())
		{
			return;
		}

		if (!WasInView && GetGlobalRect().Intersection(Main.Singleton.GetNode<Control>("%AppList").GetGlobalRect()).HasArea())
		{
			EmitSignalAppEnteredView();
			WasInView = true;
		}

		if (AppMenu.Visible && !(
				MenuButtonCancle.HasFocus() ||
				MenuButtonUpdate.HasFocus() ||
				MenuButtonCoop.HasFocus() ||
				MenuButtonStop.HasFocus() ||
				MenuButtonStart.HasFocus())
			)
		{
			AppMenu.Hide();
		}

		if (AppMenu.Visible && Input.IsActionPressed("ui_cancel"))
		{
			AppButton.GrabFocus();
		}

		if (App?.runner?.image is not null && State != AppState.DOWNLOADING)
		{
			IsImageOnDisc = await WolfAPI.IsImageOnDisk(App.runner.image);
			State = IsImageOnDisc ? RunningLobby is null ? AppState.OK : AppState.PLAYING : AppState.NOTONDISK;
		}
	}

	private void OnStateChanged()
	{
		if (State == AppState.OK)
		{
			DownloadHint.Visible = false;
			PlayingHint.Visible = false;
			OkHint.Visible = true;

			DisabledIndicator.Visible = false;
			ProgressBar.Visible = false;

			AppButton.Disabled = false;
			ProgressBar.Value = 0;

			MenuButtonStart.Text = "Start";
			MenuButtonStop.Visible = false;
			MenuButtonCoop.Disabled = false;
			MenuButtonUpdate.Disabled = false;

			MenuButtonStart.FocusNeighborBottom = MenuButtonCoop.GetPath();
			MenuButtonStart.FocusNext = MenuButtonCoop.GetPath();

			MenuButtonCoop.FocusPrevious = MenuButtonStart.GetPath();
			MenuButtonCoop.FocusNeighborTop = MenuButtonStart.GetPath();
		}
		else if (State == AppState.PLAYING)
		{
			DownloadHint.Visible = false;
			PlayingHint.Visible = true;
			OkHint.Visible = false;
			ProgressBar.Value = 0;

			if (RunningLobby is not null && !RunningLobby.multi_user)
			{
				MenuButtonStart.Text = "Connect";
				MenuButtonStop.Visible = true;
			}

			MenuButtonCoop.Disabled = true;

			MenuButtonStart.FocusNeighborBottom = MenuButtonStop.GetPath();
			MenuButtonStart.FocusNext = MenuButtonStop.GetPath();

			MenuButtonStop.FocusPrevious = MenuButtonStart.GetPath();
			MenuButtonStop.FocusNeighborTop = MenuButtonStart.GetPath();

			MenuButtonStop.FocusNeighborBottom = MenuButtonCoop.GetPath();
			MenuButtonStop.FocusNext = MenuButtonCoop.GetPath();

			MenuButtonCoop.FocusPrevious = MenuButtonStop.GetPath();
			MenuButtonCoop.FocusNeighborTop = MenuButtonStop.GetPath();
		}
		else if (State == AppState.NOTONDISK)
		{
			DownloadHint.Visible = true;
			PlayingHint.Visible = false;
			OkHint.Visible = false;
			ProgressBar.Value = 0;

			MenuButtonCoop.Disabled = true;
			MenuButtonUpdate.Disabled = true;
			MenuButtonStart.Text = "Download";

			DisabledIndicator.Visible = true;
		}
		else if (State == AppState.DOWNLOADING)
		{
			DownloadHint.Visible = true;
			PlayingHint.Visible = false;
			OkHint.Visible = false;

			ProgressBar.Visible = true;
			AppButton.Disabled = true;
			DisabledIndicator.Visible = true;
		}
	}

	private void OnPressed()
	{
		AppMenu.Visible = true;
		MenuButtonStart.GrabFocus();
	}

	private string? GetIconPath()
	{
		string icon;
		if (App.icon_png_path is null)
		{
			if (App?.runner?.image is null || !App.runner.image.Contains("ghcr.io/games-on-whales/"))
				return null;

			var name = App.runner.image.TrimPrefix("ghcr.io/games-on-whales/");
			int idx = name.LastIndexOf(':');
			if (idx >= 0)
				name = name[..idx];

			icon = $"https://games-on-whales.github.io/wildlife/apps/{name}/assets/icon.png";
		}
		else
		{
			icon = App.icon_png_path;
		}
		return icon;
	}

	private async void OnStartPressed()
	{
		//TODO: check if user already has a open singleplayer lobby for the chosen app and if yes re-join.

		if (App?.runner?.name is null)
			return;

		MenuButtonStart.Disabled = true;

		if (!IsImageOnDisc)
		{
			PullImage();
			GrabFocus();
			return;
		}


		var lobby = RunningLobby;

		var lobby_id = lobby?.id;
		if (lobby == null)
		{
			var session = await WolfAPI.GetSession();
			if (session?.client_settings is null)
				return;

			lobby = new()
			{
				profile_id = WolfAPI.Profile.id,
				name = App.title,
				multi_user = false,
				icon_png_path = GetIconPath(),
				stop_when_everyone_leaves = false,
				runner_state_folder = $"profile-data/{WolfAPI.Profile.id}/{App.runner.name}",
				runner = App.runner,
				video_settings = new()
				{
					width = session?.video_width ?? 1920,
					height = session?.video_height ?? 1080,
					refresh_rate = session?.video_refresh_rate ?? 60,
					runner_render_node = App.render_node,
					wayland_render_node = App.render_node,
					video_producer_buffer_caps = System.Environment.GetEnvironmentVariable("WOLF_VIDEO_BUFFER_CAPS") ?? ""
				},
				audio_settings = new()
				{
					channel_count = session?.audio_channel_count ?? 2
				},
				client_settings = session?.client_settings
			};
			lobby_id = await WolfAPI.CreateLobby(lobby);
		}

		State = AppState.PLAYING;


		if (lobby_id is not null)
		{
			var response = await WolfAPI.JoinLobby(lobby_id, WolfAPI.Session_id);
			if (response?.success == false)
			{
				await QuestionDialogue.OpenDialogue<bool>("Lobby full", "The Lobby you tried joining is Full.", new Dictionary<string, bool>()
				{
					{"OK", true}
				});
			}
		}


		MenuButtonStart.Disabled = false;

		AppButton.GrabFocus();
	}

	private async void OnStopPressed()
	{
		if (RunningLobby is null || RunningLobby.id is null)
			return;


		MenuButtonStop.Disabled = true;
		await WolfAPI.StopLobby(RunningLobby.id);
		MenuButtonStop.Disabled = false;

		State = IsImageOnDisc ? AppState.NOTONDISK : AppState.OK;

		AppButton.GrabFocus();
	}

	public new void GrabFocus()
	{
		AppButton.GrabFocus();
	}

	private async void OnCoopPressed()
	{
		if (App?.runner?.name is null)
			return;

		MenuButtonCoop.Disabled = true;

		var session = await WolfAPI.GetSession();
		if (session?.client_settings is null)
			return;

		Resources.WolfAPI.Lobby lobby = new()
		{
			profile_id = WolfAPI.Profile.id,
			name = App.title,
			multi_user = true,
			icon_png_path = GetIconPath(),
			stop_when_everyone_leaves = false,
			runner_state_folder = $"profile-data/{WolfAPI.Profile.id}/{App.runner.name}",
			runner = App.runner,
			video_settings = new()
			{
				width = session?.video_width ?? 1920,
				height = session?.video_height ?? 1080,
				refresh_rate = session?.video_refresh_rate ?? 60,
				runner_render_node = App.render_node,
				wayland_render_node = App.render_node,
				video_producer_buffer_caps = System.Environment.GetEnvironmentVariable("WOLF_VIDEO_BUFFER_CAPS") ?? ""
			},
			audio_settings = new()
			{
				channel_count = session?.audio_channel_count ?? 2
			},
			client_settings = session?.client_settings
		};

		if (await QuestionDialogue.OpenDialogue("Pin", "Add Pin to Lobby?",
			new Dictionary<string, bool> {
				{ "Yes", true },
				{ "No", false }
			},
			new Dictionary<string, Func<bool>>
			{
				{"No", () => { return Input.IsActionJustPressed("ui_cancel"); } }
			}
		))
		{
			List<int> pin = await PinInput.RequestPin();
			lobby.pin = pin;
		}

		var lobby_id = await WolfAPI.CreateLobby(lobby);
		if (lobby_id is not null)
			await WolfAPI.JoinLobby(lobby_id, WolfAPI.Session_id, lobby.pin);

		MenuButtonCoop.Disabled = false;

		AppButton.GrabFocus();

		State = AppState.PLAYING;
	}

	public void PullImage()
	{
		State = AppState.DOWNLOADING;
		AppButton.GrabFocus();
		if (App is null || App.runner is null || App.runner.image is null)
			return;

		WolfAPI.PullImage(App.runner.image);
	}

	public string GetFocusPath()
	{
		return GetPath();
	}
}
