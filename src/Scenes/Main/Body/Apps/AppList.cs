using Godot;
using Resources.WolfAPI;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace WolfUI;

[Tool]
public partial class AppList : Control
{
	[Export]
	Container AppContainer;
    private static readonly ILogger<AppList> Logger = WolfUI.Main.GetLogger<AppList>();
    public event EventHandler<Resources.WolfAPI.Lobby> LobbyCreatedEvent;
    public event EventHandler<string> LobbyStoppedEvent;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			AppContainer.EditorStateChanged += () =>
			{
				GD.Print("Called");
			};

			EditorMockupReady();
			return;
		}

		// Ensure at least one element is focused when switching to controller.
		Main.Singleton.controllerMap.UsedControllerChanged += (controller) =>
		{
			if (!Visible)
				return;

			var focus = Main.Singleton.GetViewport().GuiGetFocusOwner();
			if (focus is null && Main.Singleton.TopLayer.GetChildCount() <= 0)
			{
				var ctrl = (AppEntry)AppContainer.GetChildren().ToList<Node>().Find(c => c is AppEntry);
				ctrl?.GrabFocus();
				if (ctrl is null)
					GetNode<Control>("%OptionsButton").GrabFocus();
			}

		};

		VisibilityChanged += async () =>
		{
			Control BackHint = GetNode<Control>("%BackHint");
			if (Visible)
			{
				BackHint.Visible = true;
				await LoadAppList();

				var lobbies = await WolfAPI.GetLobbies();
				lobbies.ForEach(l => OnLobbyStarted(this, l));

				var ctrl = (AppEntry)AppContainer.GetChildren().ToList<Node>().Find(c => c is AppEntry);
				ctrl?.GrabFocus();
				if (ctrl is null)
					GetNode<Control>("%OptionsButton").GrabFocus();

				return;
			}
			BackHint.Hide();
		};

		WolfAPI.Singleton.LobbyCreatedEvent += OnLobbyStarted;
		WolfAPI.Singleton.LobbyStoppedEvent += OnLobbyStopped;
	}

	private void OnLobbyStopped(object caller, string lobby_id)
	{
		if (!Visible)
			return;

		LobbyStoppedEvent?.Invoke(this, lobby_id);
	}

	private void OnLobbyStarted(object sender, Resources.WolfAPI.Lobby lobby)
	{
		if (!Visible)
			return;

		if (lobby.profile_id == WolfAPI.Profile.id ||
			lobby.started_by_profile_id == WolfAPI.Profile.id
		)
		{
			LobbyCreatedEvent?.Invoke(this, lobby);
			//EmitSignalSingleplayerLobbyStarted(lobbyJson);
		}
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
		{
			return;
		}

		if (Input.IsActionJustPressed("ui_select"))
		{
			GetNode<Control>("%UserList").Visible = true;
		}

	}

	public async Task LoadAppList()
	{
		GetNode<Control>("%OptionsButton").Visible = true;
		GetNode<Label>("%HeaderLabel").Text = "Select Application";

		foreach (var child in AppContainer.GetChildren())
			child.QueueFree();

		int i = 1;
		foreach (var app in await WolfAPI.GetApps(WolfAPI.Profile))
		{
			AppEntry entry = AppEntry.Create(app);
			entry.Name = $"App {i}";
			AddAppEntry(entry);
			i++;
		}
	}

	private void EditorMockupReady()
	{
		if (AppContainer is GridContainer grid)
		{
			for (int i = 0; i < 6; i++)
			{
				for (int j = 0; j < grid.Columns; j++)
				{
					AppContainer.AddChild(AppEntry.Create(new()));
				}
			}
		}

		//AppContainer.GetChildren().ToList().ForEach(n => n.Owner = GetTree().Root);
	}

	private static Control BuildSpacer()
	{
		return new Control()
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
	}

	private void AddAppEntry(AppEntry NewAppEntry)
	{
		if (AppContainer is GridContainer gridContainer)
		{
			int AppEntryCount = gridContainer.GetChildCount();
			int GridColumns = gridContainer.Columns;

			gridContainer.AddChild(NewAppEntry);
			/*
						if (AppEntryCount < GridColumns / 2)
						{
							NewAppEntry.FocusNeighborTop = GetNode<Button>("%OptionsButton").GetPath();
						}

						if (AppEntryCount >= GridColumns / 2 && AppEntryCount < GridColumns)
						{
							NewAppEntry.FocusNeighborTop = GetNode<Button>("%ExitButton").GetPath();
						}
			*/
			if (AppEntryCount >= GridColumns)
			{
				AppEntry AboveAppEntry = gridContainer.GetChild<AppEntry>(AppEntryCount - GridColumns);
				NewAppEntry.FocusNeighborTop = AboveAppEntry.GetFocusPath();

				if (AppEntryCount % GridColumns == 0)
				{
					AppEntry AppEntry = gridContainer.GetChild<AppEntry>(AppEntryCount - 1);
					AppEntry.FocusNeighborRight = gridContainer.GetChild<AppEntry>(-1).GetFocusPath();
					gridContainer.GetChild<AppEntry>(-1).FocusNeighborLeft = AppEntry.GetFocusPath();
				}

			}
		}
	}
}
