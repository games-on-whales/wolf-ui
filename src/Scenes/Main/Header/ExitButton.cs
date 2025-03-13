using Godot;
using System;

public partial class ExitButton : Button
{
    public override void _Ready()
    {
        Pressed += () => GetTree().Quit();
        ThemeChanged += OnThemeChanged;
        OnThemeChanged();
    }

    private void OnThemeChanged()
    {
        var color = GetThemeColor("panel");
        if(color.R < 0.3f && color.G < 0.3f && color.B < 0.3f)
            InvertIconColor();
    }
    
    private void InvertIconColor()
    {
        var image = Icon.GetImage();
        for(var x = 0; x < image.GetWidth(); ++x)
        {
            for(var y = 0; y < image.GetHeight(); ++y)
            {
                var pixel = image.GetPixel(x, y);
                pixel.R = 1.0f - pixel.R;
                pixel.G = 1.0f - pixel.G;
                pixel.B = 1.0f - pixel.B;
                image.SetPixel(x, y, pixel);
            }
        }
        Icon = ImageTexture.CreateFromImage(image);
    }
}
