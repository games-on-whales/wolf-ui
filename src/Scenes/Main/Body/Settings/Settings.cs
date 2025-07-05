using Godot;
using Skerga.GodotNodeUtilGenerator;
using System.Linq;

namespace WolfUI;

[SceneAutoConfigure]
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

    public override void _Ready()
    {
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
