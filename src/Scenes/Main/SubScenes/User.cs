using Godot;
using Resources.WolfAPI;
using System;

[Tool]
public partial class User : Button
{
    public Profile profile = null;
    public override void _Ready()
    {
        GetNode<Label>("%Name").Text = Name;
        if(profile != null)
        {
            GetNode<Label>("%Name").Text = profile.name;
        }
    }
}
