using Godot;
using Resources.WolfAPI;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using UI;

[Tool]
public partial class LobbiesContainer : VBoxContainer
{
    [Export]
    PackedScene LobbyScene;
    private Control lobbies;
    public override async void _Ready()
    {
        lobbies = GetNode<Control>("%Lobbies");

        if (Engine.IsEditorHint())
        {
            EditorMockupReady();
            return;
        }

        Hide();

        WolfAPI.Singleton.LobbyCreated += (data) => CallDeferred(MethodName.OnLobbieCreated, data);
        WolfAPI.Singleton.LobbyStopped += (data) => CallDeferred(MethodName.OnLobbieStopped, data);
        lobbies.ChildEnteredTree += (node) => SetDeferred(PropertyName.Visible, true);
        lobbies.ChildExitingTree += (node) => CallDeferred(MethodName.OnChildExitingTree, node);

        var curr_lobbies = await WolfAPI.GetLobbies();
        foreach (var lobby in curr_lobbies)
        {
            AddLobby(lobby);
        }
    }

    private void EditorMockupReady()
    {
        for(var i = 0; i < 10; ++i)
        {
            var node = LobbyScene.Instantiate<Control>();
            node.Name = $"{i}";
            lobbies.AddChild(node);
        }
    }

    private void OnChildExitingTree(Node child)
    {
        GD.Print(lobbies.GetChildCount());
        if(lobbies.GetChildCount() == 0)
        {
            Hide();
        }
    }

    private void OnLobbieCreated(string lobbydata)
    {
        var lobby = JsonSerializer.Deserialize<Resources.WolfAPI.Lobby>(lobbydata);
        AddLobby(lobby);
    }

    private void AddLobby(Resources.WolfAPI.Lobby lobby)
    {
        if (lobby.multi_user == false)
            return;

        var node = LobbyScene.Instantiate<UI.Lobby>();
        node.LobbySettings = lobby;
        node.Name = lobby.id;
        node.AppName = lobby.name;
        if(lobby.profile_id != null)
            node.CreatorName = lobby.profile_id;
        if(lobby.started_by_profile_id != null)
            node.CreatorName = lobby.started_by_profile_id;
            
        lobbies.AddChild(node);
    }
    
    private void OnLobbieStopped(string lobby_id)
    {
        GD.Print($"Lobby stopped {lobby_id}");

        foreach (var node in lobbies.GetChildren())
        {
            if (node.Name == lobby_id)
            {
                lobbies.RemoveChild(node);
                node.QueueFree();
            }
        }
    }
}
