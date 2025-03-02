using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
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
	private WolfAPI wolfAPI;

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		var Main = GetNode<Main>("/root/Main");
		wolfAPI = Main.wolfAPI;

		var UserList = await wolfAPI.GetClients();

		foreach(var User in UserList)
		{
			Button button = new(){ Text = User.App_state_folder.Left(6) };
			button.Pressed += () => {
				GD.Print($"Set Selected user to {button.Text}");
				var Main = GetNode<Main>("/root/Main");
				Main.SelectedClient = User;
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
