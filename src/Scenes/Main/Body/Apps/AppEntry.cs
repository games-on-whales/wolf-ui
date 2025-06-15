using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Resources.WolfAPI;
using WolfManagement.Resources;

namespace UI
{
	[GlobalClass][Tool]
	public partial class AppEntry : Button
	{
		ProgressBar AppProgress;
		Label AppLabel;
		Control DownloadIcon;
		Control AppMenu;
		Button MenuButtonStart;
		Button MenuButtonCoop;
		Button MenuButtonUpdate;
		Button MenuButtonCancle;

		private App App;

		private bool ImageOnDisc = true;

		private DockerController docker;

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

			MenuButtonStart = GetNode<Button>("%MenuButtonStart");
			MenuButtonCoop = GetNode<Button>("%MenuButtonCoop");
			MenuButtonUpdate = GetNode<Button>("%MenuButtonUpdate");
			MenuButtonCancle = GetNode<Button>("%MenuButtonCancle");

			if (Engine.IsEditorHint())
			{
				return;
			}

			var Main = GetNode<Main>("/root/Main");
			docker = Main.docker;


			AppLabel.Text = App.title;
			DownloadIcon.Hide();
			AppProgress.Hide();
			Pressed += OnPressed;

			AppMenu.VisibilityChanged += async () =>
			{
				var curr_lobbies = await WolfAPI.GetLobbies();
				var lobby = curr_lobbies.FindAll((l) =>
				{
					bool isRunning = l.started_by_profile_id == WolfAPI.Profile.id &&
									l.name == App.title;
					return isRunning;
				}).FirstOrDefault();
				MenuButtonStart.Text = lobby == null ? "Start" : "Connect";
			};

			FocusEntered += AppMenu.Hide;
			MenuButtonCancle.Pressed += GrabFocus; //Hides menu via the FocusEntered above
			MenuButtonUpdate.Pressed += PullImage;
			MenuButtonCoop.Pressed += OnCoopPressed;
			MenuButtonStart.Pressed += OnStartPressed;

			Icon = await WolfAPI.GetAppIcon(App);
		}

		public override void _Process(double delta)
		{
			base._Process(delta);

			if (AppMenu.Visible && !(
				MenuButtonCancle.HasFocus() ||
				MenuButtonUpdate.HasFocus() ||
				MenuButtonCoop.HasFocus() ||
				MenuButtonStart.HasFocus())
			)
			{
				AppMenu.Hide();
			}

			if (AppMenu.Visible && Input.IsActionPressed("ui_cancel"))
			{
				GrabFocus();
			}

			if (App.runner.image is not null)
			{
				var app_image_name = App.runner.image.Contains(':') ? App.runner.image : $"{App.runner.image}:latest";
				var docker_images = DockerController.CachedImages;
				ImageOnDisc = docker_images.Contains(app_image_name);
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
						wayland_render_node = App.render_node
					},
					audio_settings = new()
					{
						channel_count = session.audio_channel_count
					},
					client_settings = session.client_settings
				};
				lobby_id = await WolfAPI.CreateLobby(lobby);
			}

			await WolfAPI.JoinLobby(lobby_id, WolfAPI.session_id);
			
			GrabFocus();
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
					wayland_render_node = App.render_node
				},
				audio_settings = new()
				{
					channel_count = session.audio_channel_count
				},
				client_settings = session.client_settings
			};

			var main = GetNode<Main>("/root/Main");

			if (await QuestionDialogue.OpenDialogue(main, "Pin", "Add Pin to Lobby?",
				new Dictionary<string, bool> {
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

			GrabFocus();
		}

		public async void PullImage()
		{
			GrabFocus();
			if (App.runner.image.Contains(':'))
			{
				var image = App.runner.image.Split(":");
				await docker.PullImage(image[0], image[1], AppProgress, this);
			}
		}

		public string GetFocusPath()
		{
			return GetPath();
		}
	}
}