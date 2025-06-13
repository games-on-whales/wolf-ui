using System.Collections.Generic;
using System.Linq;
using Godot;
using Resources.WolfAPI;
using UI;

public partial class AppMenu : CenterContainer
{
	[Export]
	Button StartButton;
	[Export]
	Button CoopButton;
	[Export]
	Button UpdateButton;
	[Export]
	Button CancelButton;
	AppEntry appEntry;

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		var Main = GetNode<Main>("/root/Main");

		var parent = GetParent();
		if (parent is AppEntry app)
		{
			appEntry = app;
		}

		StartButton.Pressed += OnStartPressed;
		CoopButton.Pressed += OnCoopPressed;
		UpdateButton.Pressed += OnUpdatePressed;
		CancelButton.Pressed += OnCancelPressed;
		StartButton.GrabFocus();

		var curr_lobbies = await WolfAPI.GetLobbies();
		var lobby = curr_lobbies.FindAll((l) =>
		{
			bool isRunning = l.started_by_profile_id == WolfAPI.Profile.id &&
							 l.name == appEntry.Title;
			return isRunning;
		}).FirstOrDefault();
		StartButton.Text = lobby == null ? "Start" : "Connect";
	}

	private void OnCancelPressed()
	{
		CallDeferred(MethodName.QueueFree);
		appEntry.GrabFocus();
	}

	private void OnUpdatePressed()
	{
		CallDeferred(MethodName.QueueFree);
		appEntry.GrabFocus();
		appEntry.PullImage();
	}

	private async void OnStartPressed()
	{
		//TODO: check if user already has a open singleplayer lobby for the chosen app and if yes re-join.

		var curr_lobbies = await WolfAPI.GetLobbies();
		var lobby = curr_lobbies.FindAll((l) =>
		{
			bool isRunning = l.started_by_profile_id == WolfAPI.Profile.id &&
							 l.name == appEntry.Title;
			return isRunning;
		}).FirstOrDefault();

		var sessions = await WolfAPI.GetAsync<Sessions>("http://localhost/api/v1/sessions");
		Session curr_session = null;
		foreach (var session in sessions?.sessions)
		{
			if (session.client_id == WolfAPI.session_id)
			{
				curr_session = session;
				break;
			}
		}

		if (curr_session == null)
		{
			GD.Print("No owned Session found. Is this run without Wolf?");
			curr_session = new()
			{
				video_width = 1920,
				video_height = 1080,
				video_refresh_rate = 60,
				audio_channel_count = 2,
				client_settings = new()
			};
		}

		var lobby_id = lobby?.id;
		if (lobby == null)
		{
			lobby = new()
			{
				profile_id = WolfAPI.Profile.id,
				name = appEntry.Title,
				multi_user = false,
				stop_when_everyone_leaves = false,
				runner_state_folder = $"profile-data/{WolfAPI.Profile.id}/{appEntry.runner.name}",
				runner = appEntry.runner,
				video_settings = new()
				{
					width = curr_session.video_width,
					height = curr_session.video_height,
					refresh_rate = curr_session.video_refresh_rate,
					runner_render_node = appEntry.render_node,
					wayland_render_node = appEntry.render_node
				},
				audio_settings = new()
				{
					channel_count = curr_session.audio_channel_count
				},
				client_settings = curr_session.client_settings
			};
			lobby_id = await WolfAPI.CreateLobby(lobby);
		}

		await WolfAPI.JoinLobby(lobby_id, WolfAPI.session_id);
		
		if (IsInstanceValid(this))
			QueueFree();

		appEntry.GrabFocus();
	}

	private async void OnCoopPressed()
	{
        var sessions = await WolfAPI.GetAsync<Sessions>("http://localhost/api/v1/sessions");
        Session curr_session = null;
        foreach(var session in sessions?.sessions)
        {
            if(session.client_id == WolfAPI.session_id)
            {
                curr_session = session;
                break;
            }
        }

		if (curr_session == null)
		{
			GD.Print("No owned Session found. Is this run without Wolf?");
			curr_session = new()
			{
				video_width = 1920,
				video_height = 1080,
				video_refresh_rate = 60,
				audio_channel_count = 2,
				client_settings = new()
			};
		}

		Resources.WolfAPI.Lobby lobby = new()
		{
			profile_id = WolfAPI.Profile.id,
			name = appEntry.Title,
			multi_user = true,
			stop_when_everyone_leaves = false,
			runner_state_folder = $"profile-data/{WolfAPI.Profile.id}/{appEntry.runner.name}-Coop",
			runner = appEntry.runner,
			video_settings = new()
			{
				width = curr_session.video_width,
				height = curr_session.video_height,
				refresh_rate = curr_session.video_refresh_rate,
				runner_render_node = appEntry.render_node,
				wayland_render_node = appEntry.render_node
			},
			audio_settings = new()
			{
				channel_count = curr_session.audio_channel_count
			},
			client_settings = curr_session.client_settings
		};

		var main = GetNode<Main>("/root/Main");

		if (await QuestionDialogue.OpenDialogue(main, "Pin", "Add Pin to Lobby?",
			new System.Collections.Generic.Dictionary<string, bool> {
				{ "Yes", true },
				{ "No", false }
			}
		))
		{
			List<int> pin = await PinInput.RequestPin(main);
			lobby.pin = pin;
		}

		var lobby_id = await WolfAPI.CreateLobby(lobby);
		await WolfAPI.JoinLobby(lobby_id, WolfAPI.session_id, lobby.pin);
		
		if (IsInstanceValid(this))
			QueueFree();

		appEntry.GrabFocus();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!StartButton.HasFocus() && !UpdateButton.HasFocus() && !CancelButton.HasFocus() && !CoopButton.HasFocus())
			QueueFree();
		if (Input.IsActionPressed("ui_cancel"))
		{
			QueueFree();
			appEntry.GrabFocus();
		}
	}
}
