using Godot;

public partial class ColorSelector : MarginContainer
{
    Button button;
    Slider RSlider;
    Label RLabel;
    Slider GSlider;
    Label GLabel;
    Slider BSlider;
    Label BLabel;
    ColorRect FeedbackColor;
    Label ElementName;

    [Signal]
    public delegate void ValueChangedEventHandler();
    private Color _Value;
    public Color Value
    {
        get
        {
            return _Value;
        }
        set
        {
            _Value = value;
        }
    }

    private static readonly PackedScene SelfRef = ResourceLoader.Load<PackedScene>("uid://dr7lejj7fdlux");

#nullable disable
    private ColorSelector() { }
#nullable enable

    public static ColorSelector Create()
    {
        return Create(Colors.Black);
    }

    public static ColorSelector Create(Color color)
    {
        ColorSelector obj = SelfRef.Instantiate<ColorSelector>();
        obj.Value = color;
        return obj;
    }

    public override void _Ready()
    {
        button = GetNode<Button>("%Button");

        RSlider = GetNode<Slider>("%RSlider");
        GSlider = GetNode<Slider>("%GSlider");
        BSlider = GetNode<Slider>("%BSlider");

        RLabel = GetNode<Label>("%RLabel");
        GLabel = GetNode<Label>("%GLabel");
        BLabel = GetNode<Label>("%BLabel");

        FeedbackColor = GetNode<ColorRect>("%FeedbackColor");
        ElementName = GetNode<Label>("%ElementName");

        ElementName.Text = Name;

        RSlider.Value = Value.R8;
        GSlider.Value = Value.G8;
        BSlider.Value = Value.B8;
        RLabel.Text = $"{Value.R8}";
        GLabel.Text = $"{Value.G8}";
        BLabel.Text = $"{Value.B8}";

        FeedbackColor.Color = Color.Color8((byte)RSlider.Value, (byte)GSlider.Value, (byte)BSlider.Value);

        void OnRSliderMoved(double value) => OnSliderMoved(value, RLabel);
        void OnGSliderMoved(double value) => OnSliderMoved(value, GLabel);
        void OnBSliderMoved(double value) => OnSliderMoved(value, BLabel);
        RSlider.ValueChanged += OnRSliderMoved;
        GSlider.ValueChanged += OnGSliderMoved;
        BSlider.ValueChanged += OnBSliderMoved;

        if (Engine.IsEditorHint())
            return;

        button.Pressed += () => RSlider.GrabFocus();
    }

    private void OnSliderMoved(double value, Label label)
    {
        label.Text = $"{(int)value}";
        FeedbackColor.Color = Color.Color8((byte)RSlider.Value, (byte)GSlider.Value, (byte)BSlider.Value);
        _Value = FeedbackColor.Color;
        EmitSignalValueChanged();
    }
}
