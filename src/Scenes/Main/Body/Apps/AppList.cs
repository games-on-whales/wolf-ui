using Godot;
using Resources.WolfAPI;
using System;
using System.Linq;
using System.Threading.Tasks;
using Skerga.GodotNodeUtilGenerator;
using WolfUI.Misc;

namespace WolfUI;

[Tool, GlobalClass, SceneAutoConfigure(GenerateNewMethod = false)]
public partial class AppList : Control
{
    public event EventHandler<Resources.WolfAPI.Lobby>? LobbyCreatedEvent;
    public event EventHandler<string>? LobbyStoppedEvent;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			ThemeChanged += EditorMockupReady;
			EditorMockupReady();
			return;
		}
		
		if (Main.Singleton.controllerMap is not null)
		{
			Main.Singleton.controllerMap.UsedControllerChanged += OnControllerChanged;
		}

		VisibilityChanged += RebuildAppList;
		ThemeChanged += RebuildAppList;

		WolfApi.Singleton.LobbyCreatedEvent += OnLobbyStarted;
		WolfApi.Singleton.LobbyStoppedEvent += OnLobbyStopped;
	}

	public override void _ExitTree()
	{
		WolfApi.Singleton.LobbyCreatedEvent -= OnLobbyStarted;
		WolfApi.Singleton.LobbyStoppedEvent -= OnLobbyStopped;
	}
	
	private void OnControllerChanged(ControllerMap.ControllerType  controllerType)
	{
		if (!Visible) return;
		
		// Ensure at least one element is focused when switching to controller.
		var focus = Main.Singleton.GetViewport().GuiGetFocusOwner();
		if (focus is not null || Main.Singleton.TopLayer.GetChildCount() > 0) return;

		if (AppGrid.GetChildren().Select(n => n as App).FirstOrDefault(n => n is not null) is { } ctrl)
			ctrl.GrabFocus();	
		else
			Main.Singleton.OptionsButton.GrabFocus();

	}
	
	private async void RebuildAppList()
	{
		if (!Visible)
		{
			Main.Singleton.BackHint.Hide();
			return;
		}

		AppGrid.Columns = AppGrid.GetThemeConstant("columns", "AppListGrid").Between(1, 6);
		Main.Singleton.BackHint.Visible = true;
		await LoadAppList();

		var lobbies = await WolfApi.GetLobbies();
		lobbies.ForEach(l => OnLobbyStarted(this, l));

		if (AppGrid.GetChildren().Select(n => n as App).FirstOrDefault(n => n is not null) is { } ctrl)
			ctrl.GrabFocus();	
		else
			Main.Singleton.OptionsButton.GrabFocus();
	}

	private void OnLobbyStopped(object? caller, string lobbyId)
	{
		if (!Visible)
			return;

		LobbyStoppedEvent?.Invoke(this, lobbyId);
	}

	private void OnLobbyStarted(object? sender, Resources.WolfAPI.Lobby? lobby)
	{
		if (!Visible) return;

		if (lobby?.ProfileId != WolfApi.ActiveProfile.Id &&
		    lobby?.StartedByProfileId != WolfApi.ActiveProfile.Id) return;
		if (lobby is null)
			return;
		LobbyCreatedEvent?.Invoke(this, lobby);
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
		{
			return;
		}

		if (Input.IsActionJustPressed("ui_select") && Main.Singleton.UserList is Control userList)
		{
			userList.Visible = false;
		}
	}

	private async Task LoadAppList()
	{
		Main.Singleton.OptionsButton.Visible = true;
		Main.Singleton.HeaderLabel.Text = "Loading...";

		foreach (var child in AppGrid.GetChildren())
			child.QueueFree();
		
		var enumerator = (await WolfApi.GetApps(WolfApi.ActiveProfile))
			.Select((value, i) => (value, i));
		
		foreach (var vi in enumerator)
		{
			vi.value.Name = $"App {vi.i}";
			AddAppEntry(vi.value);
		}
		
		AppGrid.GetChildren()[..AppGrid.Columns].OfType<App>().ToList().ForEach(child =>
		{
			child.AppButton.FocusEntered += () =>
			{
				AppScrollContainer.ScrollVertical = 0;
			};
		});
		var remainder = AppGrid.GetChildCount() % AppGrid.Columns;
		var idx = AppGrid.GetChildCount() - (remainder == 0 ? AppGrid.Columns : remainder);
		AppGrid.GetChildren()[idx..].OfType<App>().ToList().ForEach(child =>
		{
			child.AppButton.FocusEntered += () =>
			{
				AppScrollContainer.ScrollVertical = (int)AppScrollContainer.GetChildren().Cast<Control>().First().Size.X;
			};
		});
		
		Main.Singleton.HeaderLabel.Text = "Select Application";
	}

	private void EditorMockupReady()
	{
		
		AppGrid.Columns = AppGrid.GetThemeConstant("columns", "AppListGrid").Between(1, 6);
		foreach (var child in AppGrid.GetChildren())
			child.QueueFree();

		var scene = ResourceLoader.Load<PackedScene>("uid://chspw2lt1qcuc");
		for (var i = 0; i < 6; i++)
		{
			for (var j = 0; j < AppGrid.Columns; j++)
			{
				AppGrid.AddChild(scene.Instantiate());
			}
		}
	}

	private static Control BuildSpacer()
	{
		return new Control()
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
	}

	private void AddAppEntry(App newApp)
	{
		if (AppGrid is not { } gridContainer) return;
		var appEntryCount = gridContainer.GetChildCount();
		var gridColumns = gridContainer.Columns;

		gridContainer.AddChild(newApp);

		if (appEntryCount < gridColumns) return;
		var aboveApp = gridContainer.GetChild<App>(appEntryCount - gridColumns);
		newApp.FocusNeighborTop = aboveApp.GetPath();

		if (appEntryCount % gridColumns != 0) return;
		var app = gridContainer.GetChild<App>(appEntryCount - 1);
		app.FocusNeighborRight = gridContainer.GetChild<App>(-1).GetPath();
		gridContainer.GetChild<App>(-1).FocusNeighborLeft = app.GetPath();
	}
}
