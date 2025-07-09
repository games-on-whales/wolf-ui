using System;
using System.Collections.Generic;
using Godot;
using Resources.WolfAPI;
using Skerga.GodotNodeUtilGenerator;
using WolfUI.Misc;

namespace WolfUI;

[GlobalClass, Tool, SceneAutoConfigure]
public partial class App : MarginContainer, IRestorable<App>
{
	private enum AppState
	{
		NOTONDISK = 0,
		DOWNLOADING,
		PLAYING,
		OK,
		NONE
	}
	private AppState _state = AppState.NONE;
	private AppState State
	{
		get => _state;
		set
		{
			if (_state == value) return;
			_state = value;
			OnStateChanged();
		}
	}
	private Resources.WolfAPI.Lobby? _runningLobby;
	//private Resources.WolfAPI.AppEntry _appEntry;
	private static readonly ILogger<WolfApi> Logger = Main.GetLogger<WolfApi>();
	private bool _isImageOnDisc = true;
	
#nullable disable
	private App() { }
#nullable enable


	public static App Restore()
	{
		return Create();
	}
	
	private bool _wasInView;
	[Signal]
	private delegate void AppEnteredViewEventHandler();

	[Signal]
	private delegate void AppRunningEventHandler();

	[Signal]
	private delegate void AppStoppedEventHandler();

	private bool IsAlreadyRunning(Resources.WolfAPI.Lobby lobby)
	{
		//check if the App.Title is the same as the lobbies Name
		if (lobby.Name == Title)
			return true;
		//check if this app uses the same folder as the lobby 
		if (Runner?.Name is null || lobby.RunnerStateFolder == $"profile-data/{WolfApi.ActiveProfile.Id}/{Runner.Name}")
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

		if (AppName is null)
		{
			return;
		}
		AppName.Text = Title;
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
			State = _isImageOnDisc ? AppState.OK : AppState.NOTONDISK;
		};

		WolfApi.Singleton.ImageUpdated += OnImageUpdated;
		WolfApi.Singleton.ImageAlreadyUptoDate += OnImageUpdated;
		WolfApi.Singleton.ImagePullProgress += OnImagePullProgress;

		AppEnteredView += async () =>
		{
			AppIcon.Texture = await WolfApi.GetIcon(this);
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
			_runningLobby = lobby;
			EmitSignalAppRunning();
		}
	}

	private void OnLobbyStoppedEvent(object? caller, string lobbyId)
	{
		if (!IsInstanceValid(this))
			return;
		if (lobbyId != _runningLobby?.Id) return;
		_runningLobby = null;
		EmitSignalAppStopped();
	}

	private void OnImageUpdated(string image)
	{
		if (!IsInstanceValid(this))
			return;
		if (Runner?.Image is null || image != Runner.Image)
			return;

		State = _runningLobby is null ? AppState.OK : AppState.PLAYING;
	}

	private void OnImagePullProgress(string image, double progress)
	{
		if (Runner?.Image is null || image != Runner.Image)
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

		if (!_wasInView && GetGlobalRect().Intersection(Main.Singleton.GetNode<Control>("%AppList").GetGlobalRect()).HasArea())
		{
			EmitSignalAppEnteredView();
			_wasInView = true;
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

		if (Runner?.Image is not null && State != AppState.DOWNLOADING)
		{
			_isImageOnDisc = await WolfApi.IsImageOnDisk(Runner.Image);
			State = _isImageOnDisc ? _runningLobby is null ? AppState.OK : AppState.PLAYING : AppState.NOTONDISK;
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

			if (_runningLobby is not null && !_runningLobby.MultiUser)
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
		if (IconPngPath is null)
		{
			if (Runner?.Image is null || !Runner.Image.Contains("ghcr.io/games-on-whales/"))
				return null;

			var name = Runner.Image.TrimPrefix("ghcr.io/games-on-whales/");
			int idx = name.LastIndexOf(':');
			if (idx >= 0)
				name = name[..idx];

			icon = $"https://games-on-whales.github.io/wildlife/apps/{name}/assets/icon.png";
		}
		else
		{
			icon = IconPngPath;
		}
		return icon;
	}

	private async void OnStartPressed()
	{
		//TODO: check if user already has a open singleplayer lobby for the chosen app and if yes re-join.

		if (Runner?.Name is null)
			return;

		MenuButtonStart.Disabled = true;

		if (!_isImageOnDisc)
		{
			PullImage();
			GrabFocus();
			return;
		}


		var lobby = _runningLobby;

		var lobbyId = lobby?.Id;
		if (lobby == null)
		{
			var session = await WolfApi.GetSession();
			if (session?.ClientSettings is null)
				return;

			lobby = new Resources.WolfAPI.Lobby
			{
				ProfileId = WolfApi.ActiveProfile.Id,
				Name = Title,
				MultiUser = false,
				IconPngPath = GetIconPath(),
				StopWhenEveryoneLeaves = false,
				RunnerStateFolder = $"profile-data/{WolfApi.ActiveProfile.Id}/{Runner.Name}",
				Runner = Runner,
				VideoSettings = new VideoSettings
				{
					Width = session.VideoWidth,
					Height = session.VideoHeight,
					RefreshRate = session.VideoRefreshRate,
					RunnerRenderNode = RenderNode,
					WaylandRenderNode = RenderNode,
					VideoProducerBufferCaps = System.Environment.GetEnvironmentVariable("WOLF_VIDEO_BUFFER_CAPS") ?? ""
				},
				AudioSettings = new AudioSettings
				{
					ChannelCount = session.AudioChannelCount
				},
				ClientSettings = session.ClientSettings
			};
			lobbyId = await WolfApi.CreateLobby(lobby);
		}

		State = AppState.PLAYING;


		if (lobbyId is not null)
		{
			var response = await WolfApi.JoinLobby(lobbyId, WolfApi.SessionId);
			if (response?.Success == false)
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
		if (_runningLobby is null || _runningLobby.Id is null)
			return;


		MenuButtonStop.Disabled = true;
		await WolfApi.StopLobby(_runningLobby.Id);
		MenuButtonStop.Disabled = false;

		State = _isImageOnDisc ? AppState.NOTONDISK : AppState.OK;

		AppButton.GrabFocus();
	}

	public new void GrabFocus()
	{
		AppButton.GrabFocus();
	}

	private async void OnCoopPressed()
	{
		if (Runner?.Name is null)
			return;

		MenuButtonCoop.Disabled = true;

		var session = await WolfApi.GetSession();
		if (session?.ClientSettings is null)
			return;

		Resources.WolfAPI.Lobby lobby = new()
		{
			ProfileId = WolfApi.ActiveProfile.Id,
			Name = Title,
			MultiUser = true,
			IconPngPath = GetIconPath(),
			StopWhenEveryoneLeaves = false,
			RunnerStateFolder = $"profile-data/{WolfApi.ActiveProfile.Id}/{Runner.Name}",
			Runner = Runner,
			VideoSettings = new VideoSettings
			{
				Width = session.VideoWidth,
				Height = session.VideoHeight,
				RefreshRate = session.VideoRefreshRate,
				RunnerRenderNode = RenderNode,
				WaylandRenderNode = RenderNode,
				VideoProducerBufferCaps = System.Environment.GetEnvironmentVariable("WOLF_VIDEO_BUFFER_CAPS") ?? ""
			},
			AudioSettings = new AudioSettings
			{
				ChannelCount = session.AudioChannelCount
			},
			ClientSettings = session.ClientSettings
		};

		if (await QuestionDialogue.OpenDialogue("Pin", "Add Pin to Lobby?",
			new Dictionary<string, bool> {
				{ "Yes", true },
				{ "No", false }
			},
			new Dictionary<string, Func<bool>>
			{
				{"No", () => Input.IsActionJustPressed("ui_cancel") }
			}
		))
		{
			List<int> pin = await PinInput.RequestPin();
			lobby.Pin = pin;
		}

		var lobbyId = await WolfApi.CreateLobby(lobby);
		if (lobbyId is not null)
			await WolfApi.JoinLobby(lobbyId, WolfApi.SessionId, lobby.Pin);

		MenuButtonCoop.Disabled = false;

		AppButton.GrabFocus();

		State = AppState.PLAYING;
	}

	private void PullImage()
	{
		State = AppState.DOWNLOADING;
		AppButton.GrabFocus();
		if (Runner is null || Runner.Image is null)
			return;

		WolfApi.PullImage(Runner.Image);
	}

	public string GetFocusPath()
	{
		return GetPath();
	}
}
