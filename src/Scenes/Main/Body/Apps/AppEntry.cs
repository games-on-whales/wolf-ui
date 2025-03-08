using Godot;
using Resources.WolfAPI;
using WolfManagement.Resources;

namespace UI
{
	[GlobalClass][Tool]
	public partial class AppEntry : Button
	{
		[Export]
		ProgressBar AppProgress;
		[Export]
		Label AppLabel;
		[Export]
		public Control DownloadIcon;
		[Export]
		PackedScene AppMenu;

		private App App;
		public string Title { get{ return App.title; } }
		private string AppDisplayImagePath { get{ return App.icon_png_path ?? ""; } }
		public AppRunner runner { get{ return App.runner; } }

		public bool ImageOnDisc = true;

		private DockerController docker;

		public void Init(App app)
		{
			App = app;
		}

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			if(Engine.IsEditorHint())
			{
				return;
			}

			var Main = GetNode<Main>("/root/Main");
			docker = Main.docker;
			
			SetIcon();
			AppLabel.Text = Title;
			DownloadIcon.Visible = false;
			AppProgress.Hide();
			Pressed+= OnPressed;
		}

		private void OnPressed()
		{
			if(!ImageOnDisc)
			{
				PullImage();
			} else {
				CenterContainer scene = AppMenu.Instantiate<CenterContainer>();
				if(scene is AppMenu menu)
				{
					AddChild(menu);
					SetProcess(false);
				}
			}
		}

		public async void PullImage()
		{
			if(App.runner.image.Contains(':'))
			{
				var image = App.runner.image.Split(":");
				await docker.PullImage(image[0], image[1], AppProgress, this);
			}
		}

		private void SetChildRefrence(Node parent)
		{
			foreach(Node child in parent.GetChildren())
			{
				if(child is ProgressBar c)
					AppProgress = c;

				if(child is Label b)
					AppLabel = b;

				SetChildRefrence(child);
			}
		}

		public void SetIcon()
		{
			if(AppDisplayImagePath == "")
				return;
			
			var image = Image.LoadFromFile(AppDisplayImagePath);
			var texture = ImageTexture.CreateFromImage(image);
			Icon = texture;
		}

		public string GetFocusPath()
		{
			return GetPath();
		}
	}
}