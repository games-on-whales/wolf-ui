using Godot;
using WolfManagement.Resources;

namespace UI
{
	[GlobalClass]
	public partial class AppEntry : MarginContainer
	{
		[Export]
		public Button AppButton;
		[Export]
		ProgressBar AppProgress;
		[Export]
		Label AppLabel;
		[Export]
		DockerController docker;
		[Export]
		public Control DownloadIcon;
		[Export]
		PackedScene AppMenu;

		public Image AppImage;
		public string Title;
		public WolfApp wolfApp;
		public bool ImageOnDisc = true;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			SetChildRefrence(this);
			SetIcon();
			AppLabel.Text = Title;
			DownloadIcon.Visible = false;
			AppProgress.Hide();
			AppButton.Pressed+= OnPressed;
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
			if(wolfApp.Runner.Image.Contains(':'))
			{
				var image = wolfApp.Runner.Image.Split(":");
				await docker.PullImage(image[0], image[1], AppProgress, AppButton);
			}
		}

		private void SetChildRefrence(Node parent)
		{
			foreach(Node child in parent.GetChildren())
			{
				if(child is Button d)
					AppButton = d;

				if(child is ProgressBar c)
					AppProgress = c;

				if(child is Label b)
					AppLabel = b;

				SetChildRefrence(child);
			}
		}

		public void SetIcon()
		{
			if(AppImage != null)
			{
				var texture = ImageTexture.CreateFromImage(AppImage);
				AppButton.Icon = texture;
			}
		}

		public void SetFocus()
		{
			AppButton.GrabFocus();
		}

		public string GetFocusPath()
		{
			return AppButton.GetPath();
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}
	}
}