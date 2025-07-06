using Godot;
using Resources.WolfAPI;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WolfUI;

[Tool]
[GlobalClass]
public partial class AppList : Control
{
#nullable disable
	[Export]
	GridContainer AppContainer;
#nullable enable
    private static readonly ILogger<AppList> Logger = WolfUI.Main.GetLogger<AppList>();
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

		if (Main.Singleton?.controllerMap is not null)
		{
			// Ensure at least one element is focused when switching to controller.
			Main.Singleton.controllerMap.UsedControllerChanged += (controller) =>
			{
				if (!Visible)
					return;

				var focus = Main.Singleton.GetViewport().GuiGetFocusOwner();
				if (focus is not null || Main.Singleton.TopLayer.GetChildCount() > 0) 
					return;
				var ctrl = (App?)AppContainer.GetChildren().ToList<Node>().Find(c => c is App);
				ctrl?.GrabFocus();
				if (ctrl is null)
					GetNode<Control>("%OptionsButton").GrabFocus();

			};
		}

		VisibilityChanged += RebuildAppList;
		ThemeChanged += RebuildAppList;

		WolfApi.Singleton.LobbyCreatedEvent += OnLobbyStarted;
		WolfApi.Singleton.LobbyStoppedEvent += OnLobbyStopped;
	}

	private async void RebuildAppList()
	{
		Control BackHint = GetNode<Control>("%BackHint");
		if (Visible)
		{
			AppContainer.Columns = Math.Min(6, Math.Max(1, AppContainer.GetThemeConstant("columns", "AppListGrid")));

			BackHint.Visible = true;
			await LoadAppList();

			var lobbies = await WolfApi.GetLobbies();
			lobbies.ForEach(l => OnLobbyStarted(this, l));

			var ctrl = (App?)AppContainer.GetChildren().ToList<Node>().Find(c => c is App);
			ctrl?.GrabFocus();
			if (ctrl is null)
				GetNode<Control>("%OptionsButton").GrabFocus();

			return;
		}
		BackHint.Hide();
	}

	private void OnLobbyStopped(object? caller, string lobby_id)
	{
		if (!Visible)
			return;

		LobbyStoppedEvent?.Invoke(this, lobby_id);
	}

	private void OnLobbyStarted(object? sender, Resources.WolfAPI.Lobby? lobby)
	{
		if (!Visible)
			return;

		if (lobby?.ProfileId == WolfApi.Profile.Id ||
			lobby?.StartedByProfileId == WolfApi.Profile.Id
		)
		{
			if (lobby is null)
				return;
			LobbyCreatedEvent?.Invoke(this, lobby);
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
		foreach (var app in await WolfApi.GetApps(WolfApi.Profile))
		{
			//AppEntry entry = AppEntry.Create(app);
			app.Name = $"App {i}";
			AddAppEntry(app);
			i++;
		}

		ScrollContainer scroll = GetNode<ScrollContainer>("%AppScrollContainer");
		AppContainer.GetChildren()[..AppContainer.Columns].ToList().ForEach(child =>
		{
			child.GetNode<Button>("%AppButton").FocusEntered += () =>
			{
				scroll.ScrollVertical = 0;
			};
		});
		int remainder = AppContainer.GetChildCount() % AppContainer.Columns;
		int idx = AppContainer.GetChildCount() - (remainder == 0 ? AppContainer.Columns : remainder);
		AppContainer.GetChildren()[idx..].ToList().ForEach(child =>
		{
			child.GetNode<Button>("%AppButton").FocusEntered += () =>
			{
				scroll.ScrollVertical = (int)scroll.GetChildren().Cast<Control>().First().Size.X;
			};
		});
	}

	private void EditorMockupReady()
	{
		AppContainer.Columns = Math.Min(6, Math.Max(1, AppContainer.GetThemeConstant("columns", "AppListGrid")));
		foreach (var child in AppContainer.GetChildren())
			child.QueueFree();

		var scene = ResourceLoader.Load<PackedScene>("uid://chspw2lt1qcuc");
		for (var i = 0; i < 6; i++)
		{
			for (var j = 0; j < AppContainer.Columns; j++)
			{
				
				
				AppContainer.AddChild(scene.Instantiate());
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
		if (AppContainer is GridContainer gridContainer)
		{
			int AppEntryCount = gridContainer.GetChildCount();
			int GridColumns = gridContainer.Columns;

			gridContainer.AddChild(newApp);

			if (AppEntryCount >= GridColumns)
			{
				App aboveApp = gridContainer.GetChild<App>(AppEntryCount - GridColumns);
				newApp.FocusNeighborTop = aboveApp.GetFocusPath();

				if (AppEntryCount % GridColumns == 0)
				{
					App app = gridContainer.GetChild<App>(AppEntryCount - 1);
					app.FocusNeighborRight = gridContainer.GetChild<App>(-1).GetFocusPath();
					gridContainer.GetChild<App>(-1).FocusNeighborLeft = app.GetFocusPath();
				}

			}
		}
	}
}
