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
	private WolfAPI wolfAPI;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var Main = GetNode<Main>("/root/Main");
		wolfAPI = Main.wolfAPI;

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

	private async void OnStartPressed()
	{
		var img = appEntry.wolfApp.Runner.Image;
		var Main = GetNode<Main>("/root/Main");

		var session_id = System.Environment.GetEnvironmentVariable("WOLF_SESSION_ID");
		if(session_id == null)
		{
			GD.Print("session_id not found!");
			//return;
			session_id = Main.SelectedClient.Client_id;
		}

		await wolfAPI.StartApp(appEntry.wolfApp, true, session_id);
		GetTree().CreateTimer(1.0).Timeout += () => {
			GetTree().Quit();
		};
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
