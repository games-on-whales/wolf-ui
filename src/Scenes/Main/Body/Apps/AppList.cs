using Godot;
using Resources.WolfAPI;
using System;
using System.Linq;
using System.Text.Json;
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
				if (focus is null && Main.Singleton.TopLayer.GetChildCount() <= 0)
				{
					var ctrl = (AppEntry?)AppContainer.GetChildren().ToList<Node>().Find(c => c is AppEntry);
					ctrl?.GrabFocus();
					if (ctrl is null)
						GetNode<Control>("%OptionsButton").GrabFocus();
				}

			};
		}

		VisibilityChanged += RebuildAppList;
		ThemeChanged += RebuildAppList;

		WolfAPI.Singleton.LobbyCreatedEvent += OnLobbyStarted;
		WolfAPI.Singleton.LobbyStoppedEvent += OnLobbyStopped;
	}

	private async void RebuildAppList()
	{
		Control BackHint = GetNode<Control>("%BackHint");
		if (Visible)
		{
			AppContainer.Columns = Math.Min(6, Math.Max(1, AppContainer.GetThemeConstant("columns", "AppListGrid")));

			BackHint.Visible = true;
			await LoadAppList();

			var lobbies = await WolfAPI.GetLobbies();
			lobbies.ForEach(l => OnLobbyStarted(this, l));

			var ctrl = (AppEntry?)AppContainer.GetChildren().ToList<Node>().Find(c => c is AppEntry);
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

		if (lobby?.profile_id == WolfAPI.Profile.id ||
			lobby?.started_by_profile_id == WolfAPI.Profile.id
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
		foreach (var app in await WolfAPI.GetApps(WolfAPI.Profile))
		{
			AppEntry entry = AppEntry.Create(app);
			entry.Name = $"App {i}";
			AddAppEntry(entry);
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

		for (int i = 0; i < 6; i++)
		{
			for (int j = 0; j < AppContainer.Columns; j++)
			{
				AppContainer.AddChild(AppEntry.Create(new()));
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

	private void AddAppEntry(AppEntry NewAppEntry)
	{
		if (AppContainer is GridContainer gridContainer)
		{
			int AppEntryCount = gridContainer.GetChildCount();
			int GridColumns = gridContainer.Columns;

			gridContainer.AddChild(NewAppEntry);

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
