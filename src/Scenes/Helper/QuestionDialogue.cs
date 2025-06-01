using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public partial class QuestionDialogue : Control
{
    [Export]
    Label TitleLabel;
    [Export]
    Label ContentLabel;
    [Export]
    Control ButtonContainer;
    private readonly static PackedScene SelfRef = ResourceLoader.Load<PackedScene>("uid://bnq13qdhpc2km");
    private QuestionDialogue() { }

    public static async Task<T> OpenDialogue<T>(Node Parent, string Title, string Content, Dictionary<string, T> Choices)
    {
        if (Choices.Count <= 0)
            throw new ArgumentException("Dialogue requires at least one Choice");

        //Save current Focus, so focus can restored after
        Control FocusOwner = Parent.GetViewport().GuiGetFocusOwner();

        QuestionDialogue dialogue = SelfRef.Instantiate<QuestionDialogue>();
        dialogue.TitleLabel.Text = Title;
        dialogue.ContentLabel.Text = Content;

        Parent.AddChild(dialogue);


        T Answer = default(T);
        CancellationTokenSource Cancellation = new();

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
        if(IsInstanceValid(FocusOwner))
            FocusOwner?.GrabFocus();

        return Answer;
    }

    public override void _Ready()
    {

    }
}
