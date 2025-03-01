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
		CallDeferred(MethodName.QueueFree);
		appEntry.GrabFocus();
	}

	private void OnUpdatePressed()
	{
		CallDeferred(MethodName.QueueFree);
		appEntry.GrabFocus();
		appEntry.PullImage();
	}

	private void OnStartPressed()
	{
		var img = appEntry.wolfApp.Runner.Image;
		//TODO: Request Wolf to start the Streaming app, and Close this app
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(!StartButton.HasFocus() && !UpdateButton.HasFocus() && !CancelButton.HasFocus())
			QueueFree();
		if(Input.IsActionPressed("ui_cancel"))
		{
			QueueFree();
			appEntry.GrabFocus();
		}
	}
}
