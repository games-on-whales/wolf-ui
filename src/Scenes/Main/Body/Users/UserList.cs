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

		if (!Visible) return;
		var focus = Main.Singleton.GetViewport().GuiGetFocusOwner();
		if (focus is not null || Main.Singleton.TopLayer.GetChildCount() > 0) return;
		var ctrl = (Control?)UserContainer.GetChildren().ToList<Node>().Find(c => c is Control);
		ctrl?.GrabFocus();
	}

	private async Task LoadUsers()
	{
		Main.Singleton.OptionsButton.Visible = false;
		Main.Singleton.HeaderLabel.Text = "Loading...";

		foreach (var child in UserContainer.GetChildren())
			child.QueueFree();

		var profiles = await WolfApi.GetProfiles();

		foreach (var profile in profiles)
		{
			UserContainer.AddChild(profile);
		}

		var ch = UserContainer.GetChildren();
		if (ch.Count > 0 && ch[0] is Button b)
		{
			b.CallDeferred(Control.MethodName.GrabFocus);
		}
		Main.Singleton.HeaderLabel.Text = "Select User";
	}

	private void EditorMockupReady()
	{
		List<Profile> userList =
		[
			Profile.Create("One"),
			Profile.Create("Two"), 
			Profile.Create("Three"), 
			Profile.Create("Four"), 
			Profile.Create("Five"), 
			Profile.Create("Six"), 
			Profile.Create("Seven"), 
			Profile.Create("Eight"), 
			Profile.Create("Nine"), 
		];
		foreach(var usr in userList)
		{
			UserContainer.AddChild(usr);
		}
	}
}
