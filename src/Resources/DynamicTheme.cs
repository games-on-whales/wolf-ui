using Godot;
using System;
using System.Collections.Generic;
using System.Resources;

[GlobalClass]
public partial class DynamicTheme : Theme
{
    private static readonly Theme DefaultTheme = ResourceLoader.Load<Theme>("uid://v418qqxvwy87");

    public DynamicTheme()
    {
        MergeWith(DefaultTheme);
    }

    public DynamicTheme(Godot.Theme Base)
    {
        MergeWith(Base);
    }

    public void Save(string path)
    {
        Godot.ResourceSaver.Save(this, path);
    }
}
