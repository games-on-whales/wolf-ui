using Godot;
using Resources.WolfAPI;
using System.Collections.Generic;
using System.Linq;

//TODO Add User counter, Add check if Lobby is empty on Stop and if not ask again.
namespace WolfUI;

public partial class Lobby : Control
{
    [Signal]
    private delegate void LobbyEnteredViewEventHandler();
    private bool WasInView = false;

    [Export]
    Label AppNameLabel;
    public string AppName;
    [Export]
    Label CreatorNameLabel;
    public string CreatorName;
    [Export]
    Control MenuControl;
    [Export]
    Button LobbyMainButton;
    [Export]
    Button JoinButton;
    [Export]
    Button StopButton;
    [Export]
    Button CloseButton;
    private WolfAPI wolfAPI;
    public Resources.WolfAPI.Lobby LobbySettings;
    public override void _Ready()
    {
        if (AppNameLabel != null)
        {
            AppNameLabel.Text = AppName;
        }
        if (CreatorNameLabel != null)
        {
            CreatorNameLabel.Text = CreatorName;
        }
        if (LobbyMainButton != null)
        {
            LobbyMainButton.Pressed += OpenLobbySubMenu;
            LobbyMainButton.FocusEntered += () => MenuControl?.Hide();
        }
        if (CloseButton != null)
        {
            CloseButton.Pressed += () => LobbyMainButton.GrabFocus();
        }
        if (JoinButton != null)
        {
            JoinButton.Pressed += JoinLobby;
        }
        if (StopButton != null)
        {
            StopButton.Pressed += StopLobby;
        }

        MenuControl?.Hide();

        LobbyEnteredView += async () =>
        {
            if(LobbySettings.icon_path is not null && LobbySettings.icon_path != "")
                LobbyMainButton.Icon = await WolfAPI.GetIcon(LobbySettings.icon_path);
        };
    }

    public override void _Process(double delta)
    {
		if (!WasInView && GetGlobalRect().Intersection(Main.Singleton.GetNode<Control>("%UserList").GetGlobalRect()).HasArea())
        {
            EmitSignalLobbyEnteredView();
            WasInView = true;
        }
    }

    private void OpenLobbySubMenu()
    {
        MenuControl.Visible = true;
        JoinButton.GrabFocus();
    }

    private async void JoinLobby()
    {
        List<int> pin = null;

        if (LobbySettings.pin_required)
        {
            pin = await PinInput.RequestPin();
        }

        await WolfAPI.JoinLobby(Name, WolfAPI.session_id, pin);
    }

    private async void StopLobby()
    {
        var profiles = await WolfAPI.GetProfiles();
        var owner = profiles.FindAll(profile => profile.id == LobbySettings.started_by_profile_id
                                             || profile.id == LobbySettings.profile_id)
                            .FirstOrDefault();

        var lobbies = await WolfAPI.GetLobbies();
        var lobby = lobbies.Find(lobby => lobby.id == LobbySettings.id);

        if (owner.pin is not null && !lobby.pin_required)
        {
            var focus = GetViewport().GuiGetFocusOwner();
            await QuestionDialogue.OpenDialogue<bool>(
                "Pin required",
                "Please enter the Profiles access Pin",
                new Dictionary<string, bool>
                {
                    {"OK", false}
                });
            List<int> pin = await PinInput.RequestPin();
            if (!pin.SequenceEqual(owner.pin))
            {
                await QuestionDialogue.OpenDialogue<bool>(
                    "Incorrect Pin",
                    "The entered Pin is incorrect",
                    new Dictionary<string, bool>
                    {
                        {"OK", false}
                    });
                focus.GrabFocus();
                return;
            }
            await WolfAPI.StopLobby(Name);
        }
        else if (lobby.pin_required)
        {
            var focus = GetViewport().GuiGetFocusOwner();
            await QuestionDialogue.OpenDialogue<bool>(
                "Pin required",
                "Please enter the Lobby Pin",
                new Dictionary<string, bool>
                {
                    {"OK", false}
                });
            List<int> pin = await PinInput.RequestPin();
            await WolfAPI.StopLobby(Name, pin);
        }
        else
        {
            await WolfAPI.StopLobby(Name);
        }
    }
}