using Godot;
using Godot.Collections;

namespace WolfUI;

[GlobalClass]
[Tool]
public partial class ControllerMap : Resource
{
    public enum ControllerType { Switch, XBox, PS, None };
    public enum ControllerButton { Accept, Cancel, Up, Down, Left, Right, Back };
    private static readonly ILogger<ControllerMap> Logger = WolfUI.Main.GetLogger<ControllerMap>();
    [Export]
    Dictionary<ControllerType, Texture2D> Accept;
    [Export]
    Dictionary<ControllerType, Texture2D> Cancel;
    [Export]
    Dictionary<ControllerType, Texture2D> Up;
    [Export]
    Dictionary<ControllerType, Texture2D> Down;
    [Export]
    Dictionary<ControllerType, Texture2D> Left;
    [Export]
    Dictionary<ControllerType, Texture2D> Right;
    [Export]
    Dictionary<ControllerType, Texture2D> Back;
    private ControllerType _controller = ControllerType.None;
    private ControllerType UsedController
    {
        get
        {
            return _controller;
        }
        set
        {
            if (value != _controller)
                EmitSignalUsedControllerChanged(value);
            _controller = value;
        }
    }
    public ControllerType Controller
    {
        get
        {
            return UsedController;
        }
    }

#nullable disable // Export Variables are Godots Job
    public ControllerMap()
    {
        Input.JoyConnectionChanged += JoyConnectionChanged;
    }
#nullable enable

    private void JoyConnectionChanged(long deviceID, bool connected)
    {
        /*
        GD.Print(Input.GetJoyName((int)deviceID));
        var info = Input.GetJoyInfo((int)deviceID);
        GD.Print($"{info} ---- {(connected ? "CONNECTED" : "DISCONECTED")}");
        */
        /*
        { "vendor_id": 1406, "product_id": 8201, "raw_name": "Wolf Nintendo (virtual) pad" }
        { "vendor_id": 1118, "product_id": 746, "raw_name": "Wolf X-Box One (virtual) pad" }
        { "vendor_id": 1356, "product_id": 3302, "raw_name": "Wolf DualSense (virtual) pad" }
        */

        var oldController = UsedController;

        if (connected)
        {

            UsedController = Input.GetJoyName((int)deviceID) switch
            {
                "Xbox 360 Controller" => ControllerType.XBox,
                "Wolf X-Box One (virtual) pad" => ControllerType.XBox,
                "Wolf DualSense (virtual) pad" => ControllerType.PS,
                "Wolf Nintendo (virtual) pad" => ControllerType.Switch,
                "Nintendo Switch Pro Controller" => ControllerType.Switch,
                _ => ControllerType.XBox,
            };
        }
        if (!connected)
        {
            var ConnectedController = Input.GetConnectedJoypads();
            if (ConnectedController.Count == 0)
            {
                UsedController = ControllerType.None;
            }
        }

        if (UsedController != oldController)
        {
            Logger.LogInformation("{0} detected", Input.GetJoyName((int)deviceID));
            EmitSignalIconSetChanged();
        }

    }

    public Texture2D GetIcon(ControllerButton button)
    {
        return button switch
        {
            ControllerButton.Accept => Accept[UsedController],
            ControllerButton.Cancel => Cancel[UsedController],
            ControllerButton.Up => Up[UsedController],
            ControllerButton.Down => Down[UsedController],
            ControllerButton.Left => Left[UsedController],
            ControllerButton.Right => Right[UsedController],
            ControllerButton.Back => Back[UsedController],
            // Should never be callable but VSCode wont be fine without it
            _ => new(),
        };
    }

    public void SetController(InputEvent @event)
    {
        var oldController = UsedController;
        if (@event is InputEventMouseMotion || @event is InputEventMouseButton || @event is InputEventKey)
        {
            UsedController = ControllerType.None;
        }
        if (@event is InputEventJoypadButton || @event is InputEventJoypadMotion)
        {
            var ConnectedController = Input.GetConnectedJoypads();
            JoyConnectionChanged(ConnectedController[@event.Device], true);
            return;
        }
        if (UsedController != oldController)
            EmitSignalIconSetChanged();
    }

    [Signal]
    public delegate void IconSetChangedEventHandler();
    [Signal]
    public delegate void UsedControllerChangedEventHandler(ControllerType controller);
}
