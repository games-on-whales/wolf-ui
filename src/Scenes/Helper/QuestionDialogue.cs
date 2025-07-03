using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WolfUI;

[Tool]
public partial class QuestionDialogue : CenterContainer
{
    [Export]
    Label TitleLabel;
    [Export]
    Label ContentLabel;
    [Export]
    Control ButtonContainer;
    private readonly static PackedScene SelfRef = ResourceLoader.Load<PackedScene>("uid://bnq13qdhpc2km");
    private QuestionDialogue() { }
    private ScrollContainer scrollContainer;

    Dictionary<string, Func<bool>> Keybinds;

    [Signal]
    private delegate void KeybindPressedEventHandler(string ChoiceKey);

    public static async Task<T> OpenDialogue<T>(string Title, string Content, Dictionary<string, T> Choices, Dictionary<string, Func<bool>> Keybinds = null)
    {
        if (Choices.Count <= 0)
            throw new ArgumentException("Dialogue requires at least one Choice");

        //Save current Focus, so focus can restored after
        Control FocusOwner = Main.Singleton.GetViewport().GuiGetFocusOwner();

        QuestionDialogue dialogue = SelfRef.Instantiate<QuestionDialogue>();
        dialogue.TitleLabel.Text = Title;
        dialogue.ContentLabel.Text = Content;
        dialogue.Keybinds = Keybinds;

        Main.Singleton.TopLayer.CallDeferred(Node.MethodName.AddChild, dialogue);

        T Answer = default(T);
        CancellationTokenSource Cancellation = new();

        if (Keybinds is not null)
        {
            dialogue.KeybindPressed += (key) =>
            {
                Answer = Choices[key];
                Cancellation.Cancel();
            };
        }

        foreach (KeyValuePair<string, T> kv in Choices)
        {
            //GD.Print($"k:{kv.Key} v:{kv.Value}");
            T CapuredAnswer = kv.Value;
            Button b = new()
            {
                Text = kv.Key,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            b.Pressed += () =>
            {
                Answer = CapuredAnswer;
                Cancellation.Cancel();
            };

            dialogue.ButtonContainer.AddChild(b);
        }
        dialogue.ButtonContainer.GetChild<Button>(0).GrabFocus();

        await Task.Run(() => { Cancellation.Token.WaitHandle.WaitOne(); });

        dialogue.QueueFree();
        if (IsInstanceValid(FocusOwner))
            FocusOwner?.GrabFocus();

        return Answer;
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            return;
        }

        if (Input.IsActionPressed("ui_text_scroll_up"))
        {
            scrollContainer.ScrollVertical -= (int)(10.0 * Input.GetActionStrength("ui_text_scroll_up"));
        }
        if (Input.IsActionPressed("ui_text_scroll_down"))
        {
            scrollContainer.ScrollVertical += (int)(10.0 * Input.GetActionStrength("ui_text_scroll_down"));
        }

        if (Keybinds is null)
            return;

        foreach (var kv in Keybinds)
        {
            if (kv.Value())
            {
                EmitSignalKeybindPressed(kv.Key);
            }
        }
    }

    public override void _Ready()
    {
        scrollContainer = GetNode<ScrollContainer>("%ScrollContainer");

        if (Engine.IsEditorHint())
        {
            TitleLabel.Text = "QuestionDialogue";
            ContentLabel.Text = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.";
            Button b = new()
            {
                Text = "OK",
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            ButtonContainer.AddChild(b);
            return;
        }
    }
}
