using Godot;
using Resources.WolfAPI;
using System.Collections.Generic;
using System.Linq;
using Skerga.GodotNodeUtilGenerator;

//TODO Add User counter, Add check if Lobby is empty on Stop and if not ask again.
namespace WolfUI;

[Tool][SceneAutoConfigure]
public partial class Lobby : Control
{
    [Signal]
    private delegate void LobbyEnteredViewEventHandler();
    private bool _wasInView;
    private Resources.WolfAPI.Lobby _lobby;
#nullable disable
    private Lobby(){}
#nullable enable
    public static Lobby New(Resources.WolfAPI.Lobby lobby)
    {
        var obj = Create();
        obj._lobby = lobby;
        if (lobby.Id is null || lobby.Name is null)
            return obj;

        obj.Name = lobby.Id;
        obj.AppNameLabel.Text = lobby.Name;
        obj.CreatorNameLabel.Text = lobby.ProfileId ?? lobby.StartedByProfileId ?? "";
        return obj;
    }

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            return;
        
        LobbyMainButton.Pressed += OpenLobbySubMenu;
        LobbyMainButton.FocusEntered += () => LobbyMenu?.Hide();

        CloseButton.Pressed += LobbyMainButton.GrabFocus;
        JoinButton.Pressed += JoinLobby;
        StopButton.Pressed += StopLobby;

        LobbyMenu?.Hide();

        LobbyEnteredView += async () =>
        {
            if (_lobby.IconPngPath is not null && _lobby.IconPngPath != "")
                LobbyMainButton.Icon = await WolfApi.GetIcon(_lobby.IconPngPath);
        };
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
            return;

        var list = (UserList)Main.Singleton.UserList;
        if (_wasInView || !GetGlobalRect().Intersection(list.GetGlobalRect()).HasArea()) return;
        EmitSignalLobbyEnteredView();
        _wasInView = true;
    }

    private void OpenLobbySubMenu()
    {
        LobbyMenu.Visible = true;
        JoinButton.GrabFocus();
    }

    private async void JoinLobby()
    {
        List<int>? pin = null;

        if (_lobby.IsPinLocked)
        {
            pin = await PinInput.RequestPin();
        }

        var error = await WolfApi.JoinLobby(Name, WolfApi.SessionId, pin);
        if (error is null || error.Success) return;
        GD.Print(error.Error);
        await QuestionDialogue.OpenDialogue($"{error.Error}", $"Could not Join Lobby:\n{error.Error}.", new Dictionary<string, bool>()
        {
            {"OK", true}
        });
    }

    private async void StopLobby()
    {
        var profiles = await WolfApi.GetProfiles();
        var owner = profiles.FindAll(profile => profile.Id == _lobby.StartedByProfileId
                                             || profile.Id == _lobby.ProfileId)
                            .FirstOrDefault();

        var lobbies = await WolfApi.GetLobbies();
        var lobby = lobbies.Find(lobby => lobby.Id == _lobby.Id);

        if (lobby is not null && owner?.Pin is not null && !lobby.IsPinLocked)
        {
            var focus = GetViewport().GuiGetFocusOwner();
            _ = await QuestionDialogue.OpenDialogue(
                "Pin required",
                "Please enter the Profiles access Pin",
                new Dictionary<string, bool>
                {
                    {"OK", false}
                });
            var pin = await PinInput.RequestPin();
            if (!pin.SequenceEqual(owner.Pin))
            {
                await QuestionDialogue.OpenDialogue(
                    "Incorrect Pin",
                    "The entered Pin is incorrect",
                    new Dictionary<string, bool>
                    {
                        {"OK", false}
                    });
                focus.GrabFocus();
                return;
            }
            await WolfApi.StopLobby(Name);
        }
        else if (lobby is not null && lobby.IsPinLocked)
        {
            var focus = GetViewport().GuiGetFocusOwner();
            _ = await QuestionDialogue.OpenDialogue<bool>(
                "Pin required",
                "Please enter the Lobby Pin",
                new Dictionary<string, bool>
                {
                    {"OK", false}
                });
            var pin = await PinInput.RequestPin();
            if(IsInstanceValid(focus))
                focus.GrabFocus();
            await WolfApi.StopLobby(Name, pin);
        }
        else
        {
            await WolfApi.StopLobby(Name);
        }
    }
}