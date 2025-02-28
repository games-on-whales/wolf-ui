using Godot;
using System;

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

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			DirContents("/etc/wolf");
			var cfg = new WolfConfig();
			cfg.GetApps();


			for(int i = 0; i < 15; i++)
			{
				MarginContainer appEntry = AppEntryScene.Instantiate<MarginContainer>();
				if(appEntry is AppEntry app)
				{
					app.Name = $"AppEntry {i}";
					app.AppImage = Image.LoadFromFile("/etc/wolf/covers/kodi.png");
					AddAppEntry(app);
				}
			}

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
		public override void _Process(double delta)
		{
		}
	}
}