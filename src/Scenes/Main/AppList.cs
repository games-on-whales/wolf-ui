using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UI;
using WolfManagement.Resources;

public partial class AppList : Control
{
	[Export]
	Container AppContainer;
	[Export]
	PackedScene AppEntryScene;
	private WolfConfig config;
	private DockerController docker;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var Main = GetNode<Main>("/root/Main");
		config = Main.config;
		docker = Main.docker;

		List<WolfApp> Apps = Main.config.GetApps();

		int i = 1;
		foreach(var app in Apps)
		{
			Control appEntry = AppEntryScene.Instantiate<Control>();
			if(appEntry is AppEntry entry)
			{
				entry.Name = $"App {i}";
				entry.Title = app.Title;
				entry.wolfApp = app;
				if(app.Icon_png_path != null)
				{
					entry.AppImage = Image.LoadFromFile(app.Icon_png_path);
				}
				AddAppEntry(entry);
				i++;
			}
		}

		VisibilityChanged += () => {
			if(Visible)
			{
				if(AppContainer.GetChildCount() > 0)
					AppContainer.GetChild<AppEntry>(0).CallDeferred(AppEntry.MethodName.GrabFocus);
			}
		};
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
				string imagename = app.wolfApp.Runner.Image;
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
