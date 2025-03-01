using Godot;
using UI;

public partial class AppMenu : CenterContainer
{
	[Export]
	Button StartButton;
	[Export]
	Button UpdateButton;
	[Export]
	Button CancelButton;
	AppEntry appEntry;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var parent = GetParent();
		if(parent is AppEntry app)
		{
			appEntry = app;
		}


		StartButton.Pressed += OnStartPressed;
		UpdateButton.Pressed += OnUpdatePressed;
		CancelButton.Pressed += OnCancelPressed;
		StartButton.GrabFocus();
	}

	private void OnCancelPressed()
	{
		QueueFree();
		appEntry.AppButton.GrabFocus();
	}

	private void OnUpdatePressed()
	{
		QueueFree();
		appEntry.AppButton.GrabFocus();
		appEntry.PullImage();
	}

	private void OnStartPressed()
	{
		//TODO: Start the Image and Close the app
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(!StartButton.HasFocus() && !UpdateButton.HasFocus() && !CancelButton.HasFocus())
			QueueFree();
		if(Input.IsActionPressed("ui_cancel"))
		{
			QueueFree();
			appEntry.AppButton.GrabFocus();
		}	
	}
}
