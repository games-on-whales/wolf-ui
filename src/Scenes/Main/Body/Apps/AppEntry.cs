using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Resources.WolfAPI;
using WolfManagement.Resources;

namespace WolfUI;

[GlobalClass][Tool]
public partial class AppEntry : Control
{
	ProgressBar AppProgress;
	Label AppLabel;
	Control DownloadIcon;
	Control AppMenu;
	Button AppButton;
	Button MenuButtonStart;
	Button MenuButtonStop;
	Button MenuButtonCoop;
	Button MenuButtonUpdate;
	Button MenuButtonCancle;
	TextureRect AppIcon;

	private App App;
    private static readonly ILogger<WolfAPI> Logger = WolfUI.Main.GetLogger<WolfAPI>();
	private bool ImageOnDisc = true;

	public static readonly PackedScene SelfRef = ResourceLoader.Load<PackedScene>("uid://chspw2lt1qcuc");

	private AppEntry(){}

	public static AppEntry Create(App app)
	{
		AppEntry entry = SelfRef.Instantiate<AppEntry>();
		entry.App = app;
		return entry;
	}

	// Called when the node enters the scene tree for the first time.
	public async override void _Ready()
	{
		AppProgress = GetNode<ProgressBar>("%ProgressBar");
		AppLabel = GetNode<Label>("%AppName");
		DownloadIcon = GetNode<Control>("%DownloadHint");
		AppMenu = GetNode<Control>("%AppMenu");
		AppIcon = GetNode<TextureRect>("%AppIcon");

		AppButton = GetNode<Button>("%AppButton");
		MenuButtonStart = GetNode<Button>("%MenuButtonStart");
		MenuButtonStop = GetNode<Button>("%MenuButtonStop");
		MenuButtonCoop = GetNode<Button>("%MenuButtonCoop");
		MenuButtonUpdate = GetNode<Button>("%MenuButtonUpdate");
		MenuButtonCancle = GetNode<Button>("%MenuButtonCancle");

		MenuButtonStop.Visible = false;

		if (Engine.IsEditorHint())
		{
			return;
		}

		if (DockerController.isDisabled)
			MenuButtonUpdate.Hide();

		AppLabel.Text = App.title;
		DownloadIcon.Hide();
		AppProgress.Hide();
		AppButton.Pressed += OnPressed;

		AppMenu.VisibilityChanged += OnAppMenuVisibilityChanged;
		MenuButtonStop.VisibilityChanged += OnMenuButtonStopVisibilityChanged;

		FocusEntered += AppMenu.Hide;
		MenuButtonCancle.Pressed += AppButton.GrabFocus; //Hides menu via the FocusEntered above
		MenuButtonUpdate.Pressed += PullImage;
		MenuButtonCoop.Pressed += OnCoopPressed;
		MenuButtonStop.Pressed += OnStopPressed;
		MenuButtonStart.Pressed += OnStartPressed;

		AppIcon.Texture = await WolfAPI.GetAppIcon(App);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		if (Engine.IsEditorHint())
		{
			return;
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
			var app_image_name = App.runner.image.Contains(':') ? App.runner.image : $"{App.runner.image}:latest";
			var docker_images = DockerController.CachedImages;
			ImageOnDisc = docker_images.Contains(app_image_name) || DockerController.isDisabled;
			DownloadIcon.Visible = !ImageOnDisc;
		}
	}

	private void OnPressed()
	{
		if (!ImageOnDisc)
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

		var curr_lobbies = await WolfAPI.GetLobbies();
		var lobby = curr_lobbies.FindAll((l) =>
		{
			bool isRunning = l.started_by_profile_id == WolfAPI.Profile.id &&
							 l.multi_user == false &&
							 l.name == App.title;
			return isRunning;
		}).FirstOrDefault();

		Session session = await WolfAPI.GetSession();

		var lobby_id = lobby?.id;
		if (lobby == null)
		{
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
		
		AppButton.GrabFocus();
	}

	private async void OnStopPressed()
	{
		Logger.LogInformation("OnStopPressed");
		var curr_lobbies = await WolfAPI.GetLobbies();
		var lobby = curr_lobbies.FindAll((l) =>
		{
			bool isRunning = l.started_by_profile_id == WolfAPI.Profile.id &&
							l.name == App.title;
			return isRunning;
		}).FirstOrDefault();

		await WolfAPI.StopLobby(lobby.id);
		
		AppButton.GrabFocus();
	}

	public new void GrabFocus()
	{
		GD.Print("Tried to Grab");
		AppButton.GrabFocus();
	}

	private async void OnCoopPressed()
	{
		Session session = await WolfAPI.GetSession();

		Resources.WolfAPI.Lobby lobby = new()
		{
			profile_id = WolfAPI.Profile.id,
			name = App.title,
			multi_user = true,
			stop_when_everyone_leaves = false,
			runner_state_folder = $"profile-data/{WolfAPI.Profile.id}/{App.runner.name}-Coop",
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

		var main = GetNode<Main>("/root/Main");

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

		AppButton.GrabFocus();
	}

	public async void PullImage()
	{
		if (DockerController.isDisabled)
			return;

		AppButton.GrabFocus();
		if (App.runner.image.Contains(':'))
		{
			var image = App.runner.image.Split(":");
			await Main.Singleton.docker.PullImage(image[0], image[1], AppProgress, AppButton);
		}
	}

	private async void OnAppMenuVisibilityChanged()
	{
		if (!AppMenu.Visible)
			return;

			var curr_lobbies = await WolfAPI.GetLobbies();
			var lobby = curr_lobbies.FindAll((l) =>
		{
			bool isRunning = l.started_by_profile_id == WolfAPI.Profile.id &&
								l.multi_user == false &&
							l.name == App.title;
			return isRunning;
		}).FirstOrDefault();
		MenuButtonStart.Text = lobby == null ? "Start" : "Connect";
		MenuButtonStop.Visible = lobby != null;
	}

	private void OnMenuButtonStopVisibilityChanged()
	{
		if (MenuButtonStop.Visible)
		{
			MenuButtonStart.FocusNeighborBottom = MenuButtonStop.GetPath();
			MenuButtonStart.FocusNext = MenuButtonStop.GetPath();

			MenuButtonStop.FocusPrevious = MenuButtonStart.GetPath();
			MenuButtonStop.FocusNeighborTop = MenuButtonStart.GetPath();

			MenuButtonStop.FocusNeighborBottom = MenuButtonCoop.GetPath();
			MenuButtonStop.FocusNext = MenuButtonCoop.GetPath();

			MenuButtonCoop.FocusPrevious = MenuButtonStop.GetPath();
			MenuButtonCoop.FocusNeighborTop = MenuButtonStop.GetPath();
		}
		else
		{
			MenuButtonStart.FocusNeighborBottom = MenuButtonCoop.GetPath();
			MenuButtonStart.FocusNext = MenuButtonCoop.GetPath();

			MenuButtonCoop.FocusPrevious = MenuButtonStart.GetPath();
			MenuButtonCoop.FocusNeighborTop = MenuButtonStart.GetPath();
		}
	}

	public string GetFocusPath()
	{
		return GetPath();
	}
}
