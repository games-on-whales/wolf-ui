using Godot;
using Resources.WolfAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI;
using WolfManagement.Resources;

[Tool]
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
	private WolfAPI wolfAPI;

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		if(Engine.IsEditorHint())
		{
			EditorMockupReady();
			return;
		}

		var Main = GetNode<Main>("/root/Main");
		wolfAPI = Main.wolfAPI;

		var UserList = await WolfAPI.GetClients();

		foreach(var User in UserList)
		{
			Button button = new(){ Text = User.app_state_folder.Left(6) };
			button.Pressed += () => {
				GD.Print($"Set Selected user to {button.Text}");
				var Main = GetNode<Main>("/root/Main");
				Main.SelectedClient = User;
				AppMenu.Visible = true;
			};
			UserContainer.AddChild(button);
		}

		var ch = UserContainer.GetChildren();
		if(ch[0] is Button b)
		{
			b.CallDeferred(Button.MethodName.GrabFocus);
		}

		Visible = true;
	}

	private void EditorMockupReady()
	{
		List<Client> UserList = new(){
			new(){ app_state_folder = "One" },
			new(){ app_state_folder = "Two" },
			new(){ app_state_folder = "Three" },
			new(){ app_state_folder = "Four" },
			new(){ app_state_folder = "Five" },
			new(){ app_state_folder = "Six" },
			new(){ app_state_folder = "Seven" },
			new(){ app_state_folder = "Eight" },
			new(){ app_state_folder = "Nine" }
		};
		foreach(var User in UserList)
		{
			Button button = new(){ Text = User.app_state_folder.Left(6) };
			UserContainer.AddChild(button);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
