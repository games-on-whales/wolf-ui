using System;
using System.Diagnostics;
using Godot;
using Godot.Collections;

[GlobalClass][Tool]
public partial class ControllerMap : Resource
{
    public enum ControllerType {Switch, XBox, PS, None};
    public enum ControllerButton {Accept, Cancel, Up, Down, Left, Right};

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
    private ControllerType controller = ControllerType.None;
    public ControllerMap()
    {
        Input.JoyConnectionChanged += JoyConnectionChanged;
    }
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

        var oldController = controller;

        if(connected)
        {
            controller = Input.GetJoyName((int)deviceID) switch
            {
                "Xbox 360 Controller" => ControllerType.XBox,
                "Wolf Nintendo (virtual) pad" => ControllerType.Switch,
                "Wolf X-Box One (virtual) pad" => ControllerType.XBox,
                "Wolf DualSense (virtual) pad" => ControllerType.PS,
                _ => ControllerType.None,
            };
        }
        if(!connected)
        {
            var ConnectedController = Input.GetConnectedJoypads();
            if(ConnectedController.Count == 0)
            {
                controller = ControllerType.None;
            }
        }

        if(controller != oldController)
            EmitSignal(SignalName.IconSetChanged);
    }

    public Texture2D GetIcon(ControllerButton button)
    {
        return button switch
        {
            ControllerButton.Accept => Accept[controller],
            ControllerButton.Cancel => Cancel[controller],
            ControllerButton.Up => Up[controller],
            ControllerButton.Down => Down[controller],
            ControllerButton.Left => Left[controller],
            ControllerButton.Right => Right[controller],
            // Should never be callable but VSCode wont be fine without it
            _ => new(),
        };
    }

    [Signal]
    public delegate void IconSetChangedEventHandler();
}
