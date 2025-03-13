using Godot;
using Resources.WolfAPI;
using UI;

public partial class AppMenu : CenterContainer
{
	[Export]
	Button StartButton;
	[Export]
	Button CoopButton;
	[Export]
	Button UpdateButton;
	[Export]
	Button CancelButton;
	AppEntry appEntry;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var Main = GetNode<Main>("/root/Main");

		var parent = GetParent();
		if(parent is AppEntry app)
		{
			appEntry = app;
		}

		StartButton.Pressed += OnStartPressed;
		CoopButton.Pressed += OnCoopPressed;
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

	private async void OnStartPressed()
	{
		await WolfAPI.StartApp(appEntry.runner);
	}

	private async void OnCoopPressed()
	{
		await WolfAPI.StartApp(appEntry.runner);//TODO: Add a flag for Joinable marked Games!
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(!StartButton.HasFocus() && !UpdateButton.HasFocus() && !CancelButton.HasFocus() && !CoopButton.HasFocus())
			QueueFree();
		if(Input.IsActionPressed("ui_cancel"))
		{
			QueueFree();
			appEntry.GrabFocus();
		}
	}
}
