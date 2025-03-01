using Godot;
using System;
using System.Linq;
using UI;
using WolfManagement.Resources;

public partial class UserList : Control
{
	[Export]
	Control AppMenu;
	[Export]
	Control UserMenu;
	[Export]
	Control AppGrid;
	[Export]
	Container UserContainer;
	private WolfConfig config;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var Main = GetNode<Main>("/root/Main");
		config = Main.config;

		var UserList = config.GetUser();

		foreach(var User in UserList)
		{
			Button button = new(){ Text = User.App_state_folder.Left(6) };
			button.Pressed += () => {
				GD.Print($"Set Selected user to {button.Text}");
				UserMenu.Visible = false;
				AppMenu.Visible = true;
			};
			UserContainer.AddChild(button);
		}

		var ch = UserContainer.GetChildren();
		if(ch[0] is Button b)
		{
			b.CallDeferred(Button.MethodName.GrabFocus);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
