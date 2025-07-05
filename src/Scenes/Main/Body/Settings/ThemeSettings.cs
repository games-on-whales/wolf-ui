using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ThemeSettings : Control
{
    private static readonly PackedScene SelfRef = ResourceLoader.Load<PackedScene>("uid://bf1rpu61x753h");
#nullable disable
    private ThemeSettings() { }
#nullable enable
    private DynamicTheme ModifyableTheme;
    public static ThemeSettings Create()
    {
        return Create(new DynamicTheme());
    }
    public static ThemeSettings Create(Godot.Theme baseTheme)
    {
        var obj = SelfRef.Instantiate<ThemeSettings>();
        obj.ModifyableTheme = new DynamicTheme(baseTheme);
        return obj;
    }

    public override void _Ready()
    {
        Dictionary<string, Dictionary<Theme.DataType, string>> themeDict = [];
        for (int i = 0; i < (int)Theme.DataType.Max; ++i)
        {
            var typelist = ModifyableTheme.GetThemeItemTypeList((Theme.DataType)i);
            //GD.Print(((Theme.DataType)i).ToString());
            foreach (var t in typelist)
            {
                //GD.Print($"  {t}:");
                foreach (var e in ModifyableTheme.GetThemeItemList((Theme.DataType)i, t))
                {
                    if (!themeDict.ContainsKey(t))
                        themeDict[t] = [];

                    themeDict[t][(Theme.DataType)i] = e;
                    //GD.Print($"    {e}");
                }

            }
        }

        //GD.Print(themeDict);

        foreach (var kv in themeDict)
        {
            var parent = new GridContainer();
            var UIElementName = kv.Key;
            parent.AddChild(new Label(){ Text = UIElementName });
            foreach (var datatypeToid in kv.Value)
            {
                if (datatypeToid.Key == Theme.DataType.Color)
                {
                    InitColor(parent, UIElementName, datatypeToid.Value);
                }
            }
            GetNode<Control>("%Container").AddChild(parent);
        }
    }


    public void InitColor(Node parent, string elementName, string colorName)
    {
        var selector = ColorSelector.Create();
        selector.Name = colorName;
        selector.Value = ModifyableTheme.GetColor(colorName, elementName);
        selector.ValueChanged += () => ModifyableTheme.SetColor(colorName, elementName, selector.Value);
        parent.AddChild(selector);
    }
}
