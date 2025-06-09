using Godot;
using Resources.WolfAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UI;
using WolfManagement.Resources;

[Tool]
public partial class UserList : Control
{
	[Export]
	Control AppMenu;//{ get { return GetNode<Control>(""); } }
	private Container _UserContainer = null;
	Container UserContainer
	{
		get
		{
			if (_UserContainer != null)
				return _UserContainer;
			_UserContainer = GetNode<Container>("%UserContainer");
			return _UserContainer;
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			EditorMockupReady();
			return;
		}

		var Main = GetNode<Main>("/root/Main");

		await LoadUsers();

		Visible = true;

		VisibilityChanged += async () =>
		{
			if (Visible)
			{
				await LoadUsers();
			}
		};
	}

	private async Task LoadUsers()
	{
		GetNode<Control>("%OptionsButton").Visible = false;
		GetNode<Label>("%HeaderLabel").Text = "Select User";

		foreach(var child in UserContainer.GetChildren())
			child.QueueFree();

		var profiles = await WolfAPI.GetProfiles();

		foreach(var user in profiles)
		{
			User userNode = User.Create(user);
			userNode.Pressed += () => {
				WolfAPI.Profile = user;
				AppMenu.Visible = true;
			};
			UserContainer.AddChild(userNode);
		}

		var ch = UserContainer.GetChildren();
		if(ch[0] is Button b)
		{
			b.CallDeferred(Button.MethodName.GrabFocus);
		}
	}

	private void EditorMockupReady()
	{
		List<Profile> UserList = new(){
			new(){ name = "One" },
			new(){ name = "Two" },
			new(){ name = "Three" },
			new(){ name = "Four" },
			new(){ name = "Five" },
			new(){ name = "Six" },
			new(){ name = "Seven" },
			new(){ name = "Eight" },
			new(){ name = "Nine" }
		};
		foreach(var usr in UserList)
		{
			User user = User.Create(usr);
			UserContainer.AddChild(user);
		}
	}
}
