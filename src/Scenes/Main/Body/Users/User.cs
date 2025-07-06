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
        var obj = Create();
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
            NameLabel.Text = profile.Name;
        }

        if (Engine.IsEditorHint())
        {
            return;
        }

        UserEnteredView += async () =>
        {
            if (profile?.IconPngPath is not null && profile.IconPngPath != "")
                Icon = await WolfApi.GetIcon(profile.IconPngPath);
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
