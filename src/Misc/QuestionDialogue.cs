using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Skerga.GodotNodeUtilGenerator;

namespace WolfUI;

[Tool, SceneAutoConfigure(GenerateNewMethod = false)]
public partial class QuestionDialogue : CenterContainer
{
    private static readonly PackedScene SelfRef = ResourceLoader.Load<PackedScene>("uid://bnq13qdhpc2km");
#nullable disable
    private QuestionDialogue() { }
#nullable enable

    private Dictionary<string, Func<bool>>? _keybinds;

    [Signal]
    private delegate void KeybindPressedEventHandler(string choiceKey);

    public static async Task<T> OpenDialogue<T>(string title, string content, Dictionary<string, T> choices, Dictionary<string, Func<bool>>? keybinds = null)
    {
        if (choices.Count <= 0)
            throw new ArgumentException("Dialogue requires at least one Choice");

        //Save current Focus, so focus can be restored after
        var focusOwner = Main.Singleton.GetViewport().GuiGetFocusOwner();

        var dialogue = SelfRef.Instantiate<QuestionDialogue>();
        dialogue.TitleLabel.Text = title;
        dialogue.ContentLabel.Text = content;
        dialogue._keybinds = keybinds;

        Main.Singleton.TopLayer.CallDeferred(Node.MethodName.AddChild, dialogue);

        var answer = default(T);
        CancellationTokenSource cancellation = new();

        if (keybinds is not null)
        {
            dialogue.KeybindPressed += (key) =>
            {
                answer = choices[key];
                cancellation.Cancel();
            };
        }

        foreach (var (key, capturedAnswer) in choices)
        {
            //GD.Print($"k:{kv.Key} v:{kv.Value}");
            Button b = new()
            {
                Text = key,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            b.Pressed += () =>
            {
                answer = capturedAnswer;
                cancellation.Cancel();
            };

            dialogue.ButtonContainer.AddChild(b);
        }

        await Task.Run(() => { cancellation.Token.WaitHandle.WaitOne(); });

        dialogue.QueueFree();
        if (IsInstanceValid(focusOwner))
            focusOwner.GrabFocus();

        return answer ?? choices[choices.Keys.Last()];
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            return;
        }

        if (Input.IsActionPressed("ui_text_scroll_up"))
        {
            ScrollContainer.ScrollVertical -= (int)(10.0 * Input.GetActionStrength("ui_text_scroll_up"));
        }
        if (Input.IsActionPressed("ui_text_scroll_down"))
        {
            ScrollContainer.ScrollVertical += (int)(10.0 * Input.GetActionStrength("ui_text_scroll_down"));
        }

        if (_keybinds is null)
            return;

        foreach (var kv in _keybinds.Where(kv => kv.Value()))
        {
            EmitSignalKeybindPressed(kv.Key);
        }
    }

    public override void _Ready()
    {
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
        }
        
        ButtonContainer.GetChild<Button>(0).GrabFocus();
    }
}
