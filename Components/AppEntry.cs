using Godot;
using System;


namespace UI
{
	[GlobalClass]
	public partial class AppEntry : MarginContainer
	{
		[Export]
		Button AppButton;
		[Export]
		ProgressBar AppProgress;
		[Export]
		public Image AppImage;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			SetChildRefrence(this);

			AppProgress.Hide();
			AppButton.GuiInput+= InputRecieved;
		}

		private void InputRecieved(InputEvent input)
		{
			bool Pressed;
			if(input is InputEventMouseButton _inputEventMouseButton) {
				Pressed = _inputEventMouseButton.Pressed;
				if(_inputEventMouseButton.ButtonIndex == MouseButton.Left && !Pressed) {
					GD.Print($"{Name} recieved LEFT Mouseclick");
				}
				if(_inputEventMouseButton.ButtonIndex == MouseButton.Right && !Pressed) {
					GD.Print($"{Name} recieved RIGHT Mouseclick");
				}
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

				SetChildRefrence(child);
			}
		}

		public void SetIcon()
		{
			var texture = ImageTexture.CreateFromImage(AppImage);
			AppButton.Icon = texture;
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