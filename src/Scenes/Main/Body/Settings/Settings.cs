using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WolfUI;

[Tool]
public partial class Settings : Control
{
    [Export]
    bool SwitchSzene
    {
        get
        {
            return false;
        }
        set
        {
            ProfileSettings.Visible = !ProfileSettings.Visible;
            AppSettings.Visible = !AppSettings.Visible;
        }
    }

    //private readonly List<Node> InstanciatedNodes = [];

    Control ProfileSettings;
    Control AppSettings;

    public override void _Ready()
    {
        ProfileSettings = GetNode<Control>("%ProfileSettings");
        AppSettings = GetNode<Control>("%AppSettings");

        if (Engine.IsEditorHint())
        {
            ChildOrderChanged += EditorDynamicRebuild;
            Init();
            return;
        }

        VisibilityChanged += () =>
        {
            if (!Visible)
                return;
            Init();
        };
        Init();
    }

    private void EditorDynamicRebuild()
    {
        static void ClearOwnerlessChildren(Node parent) => parent.GetChildren().ToList().ForEach(c =>
        {
            if (c.Owner is null)
                c.QueueFree();
            else
                ClearOwnerlessChildren(c);
        });
        ClearOwnerlessChildren(this);
        Init();
    }

    private void Init()
    {
        //ProfileSettings.AddChild(ThemeSettings.Create());
    }
}
