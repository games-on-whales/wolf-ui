using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Godot;
using Resources.WolfAPI;

namespace WolfUI;

[GlobalClass][Tool]
public partial class AppEntry : MarginContainer
{
	ProgressBar AppProgress;
	Label AppLabel;
	Control DownloadIcon;
	Control OKIcon;
	Control PlayingIcon;
	Control DisabledIndicator;
	Control AppMenu;
	Button AppButton;
	Button MenuButtonStart;
	Button MenuButtonStop;
	Button MenuButtonCoop;
	Button MenuButtonUpdate;
	Button MenuButtonCancle;
	TextureRect AppIcon;
	private Resources.WolfAPI.Lobby RunningLobby = null;
	private App App;
    private static readonly ILogger<WolfAPI> Logger = WolfUI.Main.GetLogger<WolfAPI>();
	private bool IsImageOnDisc = true;

	public static readonly PackedScene SelfRef = ResourceLoader.Load<PackedScene>("uid://chspw2lt1qcuc");

	private AppEntry(){}

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
		if (lobby.runner_state_folder == $"profile-data/{WolfAPI.Profile.id}/{App.runner.name}")
			return true;

		return false;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AppProgress = GetNode<ProgressBar>("%ProgressBar");
		AppLabel = GetNode<Label>("%AppName");
		DownloadIcon = GetNode<Control>("%DownloadHint");
		OKIcon = GetNode<Control>("%OkHint");
		PlayingIcon = GetNode<Control>("%PlayingHint");
		DisabledIndicator = GetNode<Control>("%DisabledIndicator");

		AppMenu = GetNode<Control>("%AppMenu");
		AppIcon = GetNode<TextureRect>("%AppIcon");

		AppButton = GetNode<Button>("%AppButton");
		MenuButtonStart = GetNode<Button>("%MenuButtonStart");
		MenuButtonStop = GetNode<Button>("%MenuButtonStop");
		MenuButtonCoop = GetNode<Button>("%MenuButtonCoop");
		MenuButtonUpdate = GetNode<Button>("%MenuButtonUpdate");
		MenuButtonCancle = GetNode<Button>("%MenuButtonCancle");

		MenuButtonStop.Visible = false;
		DisabledIndicator.Visible = false;

		if (Engine.IsEditorHint())
		{
			return;
		}

		AppLabel.Text = App.title;
		DownloadIcon.Hide();
		AppProgress.Hide();
		AppButton.Pressed += OnPressed;

		FocusEntered += AppMenu.Hide;
		MenuButtonCancle.Pressed += AppButton.GrabFocus; //Hides menu via the FocusEntered above
		MenuButtonUpdate.Pressed += PullImage;
		MenuButtonCoop.Pressed += OnCoopPressed;
		MenuButtonStop.Pressed += OnStopPressed;
		MenuButtonStart.Pressed += OnStartPressed;

		var appList = Main.Singleton.GetNode<AppList>("%AppList");
		appList.LobbyCreatedEvent += (caller, lobby) =>
		{
			if (IsAlreadyRunning(lobby))
			{
				RunningLobby = lobby;
				EmitSignalAppRunning();
			}
		};

		appList.LobbyStoppedEvent += (caller, lobby_id) =>
		{
			if (lobby_id == RunningLobby?.id)
			{
				RunningLobby = null;
				EmitSignalAppStopped();
			}
		};

		AppRunning += () =>
		{
			if (!RunningLobby.multi_user)
			{
				MenuButtonStart.Text = "Connect";
				MenuButtonStop.Visible = true;
			}

			OKIcon.Visible = false;
			PlayingIcon.Visible = true;

			MenuButtonCoop.Disabled = true;

			MenuButtonStart.FocusNeighborBottom = MenuButtonStop.GetPath();
			MenuButtonStart.FocusNext = MenuButtonStop.GetPath();

			MenuButtonStop.FocusPrevious = MenuButtonStart.GetPath();
			MenuButtonStop.FocusNeighborTop = MenuButtonStart.GetPath();

			MenuButtonStop.FocusNeighborBottom = MenuButtonCoop.GetPath();
			MenuButtonStop.FocusNext = MenuButtonCoop.GetPath();

			MenuButtonCoop.FocusPrevious = MenuButtonStop.GetPath();
			MenuButtonCoop.FocusNeighborTop = MenuButtonStop.GetPath();
		};

		AppStopped += () =>
		{
			MenuButtonStart.Text = "Start";
			MenuButtonStop.Visible = false;

			PlayingIcon.Visible = false;
			OKIcon.Visible = IsImageOnDisc;

			MenuButtonCoop.Disabled = false;

			MenuButtonStart.FocusNeighborBottom = MenuButtonCoop.GetPath();
			MenuButtonStart.FocusNext = MenuButtonCoop.GetPath();

			MenuButtonCoop.FocusPrevious = MenuButtonStart.GetPath();
			MenuButtonCoop.FocusNeighborTop = MenuButtonStart.GetPath();
		};

		WolfAPI.Singleton.ImageUpdated += OnImageUpdated;
		WolfAPI.Singleton.ImageAlreadyUptoDate += OnImageUpdated;
		WolfAPI.Singleton.ImagePullProgress += OnImagePullProgress;

		AppEnteredView += async () =>
		{
			AppIcon.Texture = await WolfAPI.GetAppIcon(App);
		};
	}

	private void OnImageUpdated(string image)
	{
		if (image != App.runner.image)
			return;

		if (!IsInstanceValid(AppProgress) || !IsInstanceValid(AppButton) || !IsInstanceValid(DisabledIndicator))
			return;

		AppProgress.Visible = false;
		AppButton.Disabled = false;
		DisabledIndicator.Visible = false;

		DownloadIcon.Visible = false;
		OKIcon.Visible = !PlayingIcon.Visible;
		AppProgress.Value = 0;
	}

	private void OnImagePullProgress(string image, double progress)
	{
		if (image != App.runner.image)
			return;

		if (!IsInstanceValid(AppProgress) || !IsInstanceValid(AppButton) || !IsInstanceValid(DisabledIndicator))
			return;

		if (!AppProgress.Visible || !AppButton.Disabled || !DisabledIndicator.Visible)
		{
			AppProgress.Visible = true;
			AppButton.Disabled = true;
			DisabledIndicator.Visible = true;
		}				

		AppProgress.Value = progress;
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

		if (App.runner.image is not null)
		{
			IsImageOnDisc = await WolfAPI.IsImageOnDisk(App.runner.image);
			DownloadIcon.Visible = !IsImageOnDisc;
			OKIcon.Visible = IsImageOnDisc;
		}
	}

	private void OnPressed()
	{
		if (!IsImageOnDisc)
		{
			PullImage();
		}
		else
		{
			AppMenu.Visible = true;
			MenuButtonStart.GrabFocus();
		}
	}

	private async void OnStartPressed()
	{
		//TODO: check if user already has a open singleplayer lobby for the chosen app and if yes re-join.

		MenuButtonStart.Disabled = true;

		var lobby = RunningLobby;

		var lobby_id = lobby?.id;
		if (lobby == null)
		{
			Session session = await WolfAPI.GetSession();

			lobby = new()
			{
				profile_id = WolfAPI.Profile.id,
				name = App.title,
				multi_user = false,
				stop_when_everyone_leaves = false,
				runner_state_folder = $"profile-data/{WolfAPI.Profile.id}/{App.runner.name}",
				runner = App.runner,
				video_settings = new()
				{
					width = session.video_width,
					height = session.video_height,
					refresh_rate = session.video_refresh_rate,
					runner_render_node = App.render_node,
					wayland_render_node = App.render_node,
					video_producer_buffer_caps = System.Environment.GetEnvironmentVariable("WOLF_VIDEO_BUFFER_CAPS") ?? ""
				},
				audio_settings = new()
				{
					channel_count = session.audio_channel_count
				},
				client_settings = session.client_settings
			};
			lobby_id = await WolfAPI.CreateLobby(lobby);
		}

		if (lobby_id is not null)
			await WolfAPI.JoinLobby(lobby_id, WolfAPI.session_id);
		
		MenuButtonStart.Disabled = false;

		AppButton.GrabFocus();
	}

	private async void OnStopPressed()
	{
		if (RunningLobby is null)
			return;


		MenuButtonStop.Disabled = true;
		await WolfAPI.StopLobby(RunningLobby.id);
		MenuButtonStop.Disabled = false;

		AppButton.GrabFocus();
	}

	public new void GrabFocus()
	{
		AppButton.GrabFocus();
	}

	private async void OnCoopPressed()
	{
		MenuButtonCoop.Disabled = true;

		Session session = await WolfAPI.GetSession();

		Resources.WolfAPI.Lobby lobby = new()
		{
			profile_id = WolfAPI.Profile.id,
			name = App.title,
			multi_user = true,
			stop_when_everyone_leaves = false,
			runner_state_folder = $"profile-data/{WolfAPI.Profile.id}/{App.runner.name}",
			runner = App.runner,
			video_settings = new()
			{
				width = session.video_width,
				height = session.video_height,
				refresh_rate = session.video_refresh_rate,
				runner_render_node = App.render_node,
				wayland_render_node = App.render_node,
				video_producer_buffer_caps = System.Environment.GetEnvironmentVariable("WOLF_VIDEO_BUFFER_CAPS") ?? ""
			},
			audio_settings = new()
			{
				channel_count = session.audio_channel_count
			},
			client_settings = session.client_settings
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
			await WolfAPI.JoinLobby(lobby_id, WolfAPI.session_id, lobby.pin);

		MenuButtonCoop.Disabled = false;

		AppButton.GrabFocus();
	}

	public void PullImage()
	{
		AppButton.GrabFocus();

		AppProgress.Visible = true;
		AppButton.Disabled = true;
		DisabledIndicator.Visible = true;
		AppProgress.Value = 0;

		WolfAPI.PullImage(App.runner.image);
	}

	public string GetFocusPath()
	{
		return GetPath();
	}
}
