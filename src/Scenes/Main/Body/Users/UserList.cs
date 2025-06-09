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
	[Export]
	PackedScene UserEntry;

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		if(Engine.IsEditorHint())
		{
			EditorMockupReady();
			return;
		}

		var Main = GetNode<Main>("/root/Main");

		await LoadUsers();

		Visible = true;

		VisibilityChanged += async () => {
			if(Visible)
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

		foreach(var User in profiles)
		{
			Button button = UserEntry.Instantiate<Button>();
			if(button is User user)
			{
				user.profile = User;
			}
			//Button button = new(){ Text = User.name };
			button.Pressed += () => {
				WolfAPI.Profile = User;
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
		foreach(var User in UserList)
		{
			Button button = UserEntry.Instantiate<Button>();
			button.Name = User.name;
			UserContainer.AddChild(button);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
