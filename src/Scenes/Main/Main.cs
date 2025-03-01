using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WolfManagement.Resources;

namespace UI
{
	public partial class Main : Control
	{
		[Export]
		Button ExitButton;
		[Export]
		Button OptionsButton;
		[Export]
		GridContainer AppGrid;
		[Export]
		PackedScene AppEntryScene;
		[Export]
		PackedScene AddAppScene;

		[Export]
		DockerController docker;
		[Export]
		WolfConfig config;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			List<WolfApp> Apps = config.GetApps();

			int i = 1;
			foreach(var app in Apps)
			{
				MarginContainer appEntry = AppEntryScene.Instantiate<MarginContainer>();
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

			MarginContainer addAppEntry = AddAppScene.Instantiate<MarginContainer>();
			AppGrid.AddChild(addAppEntry);

			if(AppGrid.GetChildCount() > 0)
				AppGrid.GetChild<AppEntry>(0).SetFocus();

			//AppGrid.GetChild<AppEntry>(0).FocusNeighborTop = OptionsButton.GetPath();
		}

		public void DirContents(string path)
		{
			using var dir = DirAccess.Open(path);
			if (dir != null)
			{
				dir.ListDirBegin();
				string fileName = dir.GetNext();
				while (fileName != "")
				{
					if (dir.CurrentIsDir())
					{
						GD.Print($"Found directory: {fileName}");
					}
					else
					{
						GD.Print($"Found file: {fileName}");
					}
					fileName = dir.GetNext();
				}
			}
			else
			{
				GD.Print("An error occurred when trying to access the path.");
			}
		}

		private void AddAppEntry(AppEntry NewAppEntry)
		{
			int AppEntryCount = AppGrid.GetChildCount();
			int GridColumns = AppGrid.Columns;

			AppGrid.AddChild(NewAppEntry);

			if(AppEntryCount >= GridColumns)
			{
				AppEntry AboveAppEntry = AppGrid.GetChild<AppEntry>(AppEntryCount - GridColumns);
				NewAppEntry.FocusNeighborTop = AboveAppEntry.GetFocusPath();

				if(AppEntryCount % GridColumns == 0)
				{
					AppEntry AppEntry = AppGrid.GetChild<AppEntry>(AppEntryCount - 1);
					AppEntry.FocusNeighborRight = AppGrid.GetChild<AppEntry>(-1).GetFocusPath();
					AppGrid.GetChild<AppEntry>(-1).FocusNeighborLeft = AppEntry.GetFocusPath();
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

			foreach(var child in AppGrid.GetChildren())
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
}