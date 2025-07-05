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
    private bool WasInView = false;
    public string AppName = string.Empty;
    public string CreatorName = string.Empty;
    public Resources.WolfAPI.Lobby LobbySettings;
#nullable disable
    private Lobby(){}
#nullable enable
    public static Lobby New(Resources.WolfAPI.Lobby lobby)
    {
        var obj = New();
        obj.LobbySettings = lobby;
        if (lobby?.id is null || lobby?.name is null)
            return obj;

        obj.Name = lobby.id;
        obj.AppName = lobby.name;
        return obj;
    }

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            return;

        AppNameLabel.Text = AppName;
        CreatorNameLabel.Text = CreatorName;

        LobbyMainButton.Pressed += OpenLobbySubMenu;
        LobbyMainButton.FocusEntered += () => LobbyMenu?.Hide();

        CloseButton.Pressed += LobbyMainButton.GrabFocus;
        JoinButton.Pressed += JoinLobby;
        StopButton.Pressed += StopLobby;

        LobbyMenu?.Hide();

        LobbyEnteredView += async () =>
        {
            if (LobbySettings.icon_png_path is not null && LobbySettings.icon_png_path != "")
                LobbyMainButton.Icon = await WolfAPI.GetIcon(LobbySettings.icon_png_path);
        };
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
            return;


        if (!WasInView && GetGlobalRect().Intersection(Main.Singleton.GetNode<Control>("%UserList").GetGlobalRect()).HasArea())
            {
                EmitSignalLobbyEnteredView();
                WasInView = true;
            }
    }

    private void OpenLobbySubMenu()
    {
        LobbyMenu.Visible = true;
        JoinButton.GrabFocus();
    }

    private async void JoinLobby()
    {
        List<int>? pin = null;

        if (LobbySettings.pin_required)
        {
            pin = await PinInput.RequestPin();
        }

        var error = await WolfAPI.JoinLobby(Name, WolfAPI.Session_id, pin);
        if (error is not null && error.success == false)
        {
            GD.Print(error.error);
            await QuestionDialogue.OpenDialogue<bool>($"{error.error}", $"Could not Join Lobby:\n{error.error}.", new Dictionary<string, bool>()
            {
                {"OK", true}
            });
        }
    }

    private async void StopLobby()
    {
        var profiles = await WolfAPI.GetProfiles();
        var owner = profiles.FindAll(profile => profile.id == LobbySettings.started_by_profile_id
                                             || profile.id == LobbySettings.profile_id)
                            .FirstOrDefault();

        var lobbies = await WolfAPI.GetLobbies();
        var lobby = lobbies.Find(lobby => lobby.id == LobbySettings.id);

        if (lobby is not null && owner?.pin is not null && !lobby.pin_required)
        {
            var focus = GetViewport().GuiGetFocusOwner();
            _ = await QuestionDialogue.OpenDialogue<bool>(
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
        else if (lobby is not null && lobby.pin_required)
        {
            var focus = GetViewport().GuiGetFocusOwner();
            _ = await QuestionDialogue.OpenDialogue<bool>(
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