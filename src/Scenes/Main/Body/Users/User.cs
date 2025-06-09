using Godot;
using Resources.WolfAPI;
using System;

[Tool]
public partial class User : Button
{
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
    }
}
