using Godot;

[GlobalClass]
public partial class DynamicTheme : Theme
{
    private static readonly Theme DefaultTheme = ResourceLoader.Load<Theme>("uid://v418qqxvwy87");

    public DynamicTheme()
    {
        MergeWith(DefaultTheme);
    }

    public DynamicTheme(Theme @base)
    {
        MergeWith(@base);
    }

    public void Save(string path)
    {
        Godot.ResourceSaver.Save(this, path);
    }
}
