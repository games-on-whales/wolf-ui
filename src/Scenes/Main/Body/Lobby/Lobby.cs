using Godot;
using Resources.WolfAPI;
using System;

//TODO Add User counter, Add check if Lobby is empty on Stop and if not ask again.
namespace UI
{
    public partial class Lobby : Control
    {
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
        }

        private void OpenLobbySubMenu()
        {
            MenuControl.Visible = true;
            JoinButton.GrabFocus();
        }

        private async void JoinLobby()
        {
            await WolfAPI.JoinLobby(Name, WolfAPI.session_id);
        }

        private async void StopLobby()
        {
            await WolfAPI.StopLobby(Name);
        }
    }
}