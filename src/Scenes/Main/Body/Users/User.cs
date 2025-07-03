using Godot;
using Resources.WolfAPI;

namespace WolfUI;

[Tool]
public partial class User : Button
{
    [Signal]
    private delegate void UserEnteredViewEventHandler();
    private bool WasInView = false;

    public Profile profile = null;
    private static readonly PackedScene SelfRef = ResourceLoader.Load<PackedScene>("uid://i2pa0j4ijr4s");
    public static User Create(Profile profile)
    {
        User user = SelfRef.Instantiate<User>();

        user.profile = profile;
        return user;
    }

    private User() { }

    public override void _Ready()
    {
        GetNode<Label>("%Name").Text = Name;
        if (profile != null)
        {
            GetNode<Label>("%Name").Text = profile.name;
        }
        
		if (Engine.IsEditorHint())
        {
            return;
        }

        UserEnteredView += async () =>
        {
            if(profile.icon_png_path != "")
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
