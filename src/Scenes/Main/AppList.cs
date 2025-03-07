using Godot;
using Resources.WolfAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI;
using WolfManagement.Resources;

[Tool]
public partial class AppList : Control
{
	[Export]
	Container AppContainer;
	[Export]
	PackedScene AppEntryScene;
	private DockerController docker;
	private WolfAPI wolfAPI;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if(Engine.IsEditorHint())
		{
			EditorMockupReady();
			return;
		}

		var Main = GetNode<Main>("/root/Main");
		docker = Main.docker;
		wolfAPI = Main.wolfAPI;

		VisibilityChanged += async () => {
			if(Visible)
			{
				await LoadAppList();
				if(AppContainer.GetChildCount() > 0)
				{
					AppContainer.GetChild<AppEntry>(0).CallDeferred(AppEntry.MethodName.GrabFocus);
				} else {
					GetNode<Control>("%OptionsButton").GrabFocus();
				}
			}
		};
	}

	public async Task LoadAppList()
	{
		foreach(var child in AppContainer.GetChildren())
			child.QueueFree();

		var Main = GetNode<Main>("/root/Main");
		var profile = Main.SelectedProfile;
		List<App> Apps = await WolfAPI.GetApps(profile);//Main.config.GetApps();

		int i = 1;
		foreach(var app in Apps)
		{
			Control appEntry = AppEntryScene.Instantiate<Control>();
			if(appEntry is AppEntry entry)
			{
				entry.Init(app);
				entry.Name = $"App {i}";
				AddAppEntry(entry);
				i++;
			}
		}
	}

	private void EditorMockupReady()
	{
		for(int n = 0; n < 23; n++)
		{
			Control appEntry = AppEntryScene.Instantiate<Control>();
			if(appEntry is AppEntry entry)
			{
				AppContainer.AddChild(entry);
			}
		}
	}

	private void AddAppEntry(AppEntry NewAppEntry)
	{
		if(AppContainer is GridContainer gridContainer)
		{
			int AppEntryCount = gridContainer.GetChildCount();
			int GridColumns = gridContainer.Columns;

			gridContainer.AddChild(NewAppEntry);

			if(AppEntryCount >= GridColumns)
			{
				AppEntry AboveAppEntry = gridContainer.GetChild<AppEntry>(AppEntryCount - GridColumns);
				NewAppEntry.FocusNeighborTop = AboveAppEntry.GetFocusPath();

				if(AppEntryCount % GridColumns == 0)
				{
					AppEntry AppEntry = gridContainer.GetChild<AppEntry>(AppEntryCount - 1);
					AppEntry.FocusNeighborRight = gridContainer.GetChild<AppEntry>(-1).GetFocusPath();
					gridContainer.GetChild<AppEntry>(-1).FocusNeighborLeft = AppEntry.GetFocusPath();
				}

			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public async override void _Process(double delta)
	{
		if(Engine.IsEditorHint())
			return;

		var Images = await docker.ListImages();
		HashSet<string> existingImages = new();
		foreach(var image in Images)
		{
			if(image.RepoTags.Count > 0)
			{
				existingImages.Add(image.RepoTags.First<string>());
			}
		}

		foreach(var child in AppContainer.GetChildren())
		{
			if(child is AppEntry app)
			{
				if(app.runner.image == null)
					return;

				string imagename = app.runner.image;
				if(!imagename.Contains(':'))
					imagename = $"{imagename}:latest";
				if(existingImages.Contains(imagename))
				{
					app.ImageOnDisc = true;
					app.DownloadIcon.Visible = false;
				} else {
					app.ImageOnDisc = false;
					app.DownloadIcon.Visible = true;
				}
			}
		}
	}
}
