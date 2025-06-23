using Godot;
using Resources.WolfAPI;
using System.Threading.Tasks;

namespace WolfUI;

[Tool]
public partial class AppList : Control
{
	[Export]
	Container AppContainer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if(Engine.IsEditorHint())
		{
			EditorMockupReady();
			return;
		}

		VisibilityChanged += async () =>
		{
			Control BackHint = GetNode<Control>("%BackHint");
			if (Visible)
			{
				BackHint.Visible = true;
				await LoadAppList();
				if (AppContainer.GetChildCount() > 0)
				{
					AppContainer.GetChild<AppEntry>(0).CallDeferred(AppEntry.MethodName.GrabFocus);
				}
				else
				{
					GetNode<Control>("%OptionsButton").GrabFocus();
				}
				return;
			}
			BackHint.Hide();
		};
	}

	public override void _Process(double delta)
	{
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
		for(int n = 0; n < 23; n++)
		{
			AppEntry entry = AppEntry.Create(new());
			AppContainer.AddChild(entry);
		}
	}

	private void AddAppEntry(AppEntry NewAppEntry)
	{
		if(AppContainer is GridContainer gridContainer)
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
