using Godot;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WolfUI;

public partial class PinInput : CenterContainer
{
    [Export]
    LineEdit PinLineEdit;
    [Export]
    Button BackButton;
    [Export]
    Button ClearButton;
    [Export]
    Button AcceptButton;
    [Export]
    Button CancelButton;

    private bool canceled = false;
    private static readonly PackedScene SelfRef = ResourceLoader.Load<PackedScene>("uid://cmeq4kkqbu0iw");
    readonly CancellationTokenSource cancelSource = new();
    List<Button> NumberButtons;

    public static async Task<List<int>> RequestPin()
    {
        Control FocusOwner = Main.Singleton.GetViewport().GuiGetFocusOwner();

        PinInput node = SelfRef.Instantiate<PinInput>();

        Main.Singleton.TopLayer.AddChild(node);

        List<int> ints = await Task.Run(node.GetPinBlocking);
        node.QueueFree();

        if (FocusOwner != null && IsInstanceValid(FocusOwner))
            FocusOwner.GrabFocus();

        return ints;
    }

    private PinInput() { }

    private List<int> GetPinBlocking()
    {
        cancelSource.Token.WaitHandle.WaitOne();

        if (canceled)
            return [];

        List<int> ints = [];
        var PinText = PinLineEdit.Text;
        foreach (char c in PinText)
        {
            ints.Add(int.Parse(c.ToString()));
        }

        return ints;
    }

    public override void _Ready()
    {
        if (PinLineEdit == null)
            return;

        PinLineEdit.GrabFocus();

        NumberButtons = [];
        for (int i = 0; i < 10; i++)
        {
            NumberButtons.Add(GetNode<Button>($"%Button{i}"));
        }


        PinLineEdit.GuiInput += @event =>
        {
            if (@event.IsActionPressed("ui_accept"))
            {
                PinLineEdit.ReleaseFocus();
                cancelSource.Cancel();
            }

            if (@event.IsActionPressed("ui_down"))
            {
                NumberButtons[1].GrabFocus();
            }

            if (NumericRegex().Matches(@event.AsText()).Count <= 0)
            {
                // Stop event from reaching the UI.
                PinLineEdit.AcceptEvent();
            }
        };

        if (NumberButtons != null)
        {
            int i = 0;
            foreach (Button button in NumberButtons)
            {
                int num = i;
                button.Pressed += () =>
                {
                    PinLineEdit.Text = $"{PinLineEdit.Text}{num}";
                };
                i++;
            }
        }

        BackButton.Pressed += () =>
        {
            PinLineEdit.Text = $"{PinLineEdit.Text.Left(PinLineEdit.Text.Length - 1)}";
        };

        ClearButton.Pressed += () =>
        {
            PinLineEdit.Text = "";
        };

        AcceptButton.Pressed += () =>
        {
            cancelSource.Cancel();
        };

        CancelButton.Pressed += () =>
        {
            canceled = true;
            cancelSource.Cancel();
        };
    }

    [GeneratedRegex("[0-9]")]
    private static partial Regex NumericRegex();
}
