using Godot;
using Resources.WolfAPI;
using Skerga.GodotNodeUtilGenerator;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WolfUI;

[Tool][SceneAutoConfigure]
public partial class UserList : Control
{
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

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
		{
			return;
		}
		if (Visible)
		{
			var focus = Main.Singleton.GetViewport().GuiGetFocusOwner();
			if (focus is null && Main.Singleton.TopLayer.GetChildCount() <= 0)
			{
				var ctrl = (Control?)UserContainer.GetChildren().ToList<Node>().Find(c => c is Control);
				ctrl?.GrabFocus();
			}
		}
    }

	private async Task LoadUsers()
	{
		GetNode<Control>("%OptionsButton").Visible = false;
		GetNode<Label>("%HeaderLabel").Text = "Select User";

		foreach (var child in UserContainer.GetChildren())
			child.QueueFree();

		var profiles = await WolfApi.GetProfiles();
		if (profiles is null)
			return;

		foreach (var profile in profiles)
		{
			User userNode = User.New(profile);
			userNode.Pressed += async () =>
			{
				if (profile.Pin is not null)
				{
					var focus = GetViewport().GuiGetFocusOwner();
					List<int> pin = await PinInput.RequestPin();
					if (!pin.SequenceEqual(profile.Pin))
					{
						await QuestionDialogue.OpenDialogue<bool>(
							"Incorrect Pin",
							"The entered Pin is incorrect",
							new Dictionary<string, bool>
							{
							{"OK", false}
							});
						focus.GrabFocus();
						return;
					}
				}

				WolfApi.Profile = profile;

				if(Main.Singleton.AppList is AppList appmenu)
					appmenu.Visible = true;
			};
			UserContainer.AddChild(userNode);
		}

		var ch = UserContainer.GetChildren();
		if (ch.Count > 0 && ch[0] is Button b)
		{
			b.CallDeferred(Button.MethodName.GrabFocus);
		}
	}

	private void EditorMockupReady()
	{
		List<Profile> UserList = new(){
			new(){ Name = "One" },
			new(){ Name = "Two" },
			new(){ Name = "Three" },
			new(){ Name = "Four" },
			new(){ Name = "Five" },
			new(){ Name = "Six" },
			new(){ Name = "Seven" },
			new(){ Name = "Eight" },
			new(){ Name = "Nine" }
		};
		foreach(var usr in UserList)
		{
			User user = User.New(usr);
			UserContainer.AddChild(user);
		}
	}
}
