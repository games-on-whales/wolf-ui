using Godot;
using Resources.WolfAPI;
using Skerga.GodotNodeUtilGenerator;

namespace WolfUI;

[Tool][SceneAutoConfigure]
public partial class User : Button
{
    [Signal]
    private delegate void UserEnteredViewEventHandler();
    private bool WasInView = false;
    public Profile profile;

    public static User New(Profile profile)
    {
        var obj = New();
        obj.profile = profile;
        return obj;
    }
#nullable disable
    private User() { }
#nullable enable

    public override void _Ready()
    {
        NameLabel.Text = Name;
        if (profile != null)
        {
            NameLabel.Text = profile.name;
        }

        if (Engine.IsEditorHint())
        {
            return;
        }

        UserEnteredView += async () =>
        {
            if (profile?.icon_png_path is not null && profile.icon_png_path != "")
                Icon = await WolfAPI.GetIcon(profile.icon_png_path);
        };
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
		{
			return;
		}


		if (!WasInView && GetGlobalRect().Intersection(Main.Singleton.GetNode<Control>("%UserList").GetGlobalRect()).HasArea())
        {
            EmitSignalUserEnteredView();
            WasInView = true;
        }
    }
}
