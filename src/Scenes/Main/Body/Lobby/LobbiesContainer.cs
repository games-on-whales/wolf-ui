using Godot;
using Resources.WolfAPI;
using Skerga.GodotNodeUtilGenerator;
using System.Text.Json;

namespace WolfUI;

[Tool][SceneAutoConfigure]
public partial class LobbiesContainer : VBoxContainer
{
    private static readonly ILogger<LobbiesContainer> Logger = WolfUI.Main.GetLogger<LobbiesContainer>();
    public override async void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            EditorMockupReady();
            return;
        }

        Hide();

        WolfAPI.Singleton.LobbyCreatedEvent += AddLobby;
        WolfAPI.Singleton.LobbyStoppedEvent += OnLobbieStopped;
        Lobbies.ChildEnteredTree += (node) => SetDeferred(PropertyName.Visible, true);
        Lobbies.ChildExitingTree += (node) => CallDeferred(MethodName.OnChildExitingTree, node);

        var curr_lobbies = await WolfAPI.GetLobbies();
        foreach (var lobby in curr_lobbies)
        {
            AddLobby(this, lobby);
        }
    }

    private void EditorMockupReady()
    {
        for(var i = 0; i < 10; ++i)
        {
            var node = Lobby.New();
            node.Name = $"{i}";
            Lobbies.AddChild(node);
        }
    }

    private void OnChildExitingTree(Node child)
    {
        //Logger.LogInformation("{Msg}", lobbies.GetChildCount());
        //GD.Print(lobbies.GetChildCount());
        if(Lobbies.GetChildCount() == 0)
        {
            Hide();
        }
    }

    private void AddLobby(object? caller, Resources.WolfAPI.Lobby lobby)
    {
        if (lobby.multi_user == false)
            return;

        var node = Lobby.New(lobby);
        if(lobby.profile_id != null)
            node.CreatorName = lobby.profile_id;
        if(lobby.started_by_profile_id != null)
            node.CreatorName = lobby.started_by_profile_id;
            
        Lobbies.AddChild(node);
    }
    
    private void OnLobbieStopped(object? caller, string lobby_id)
    {
        Logger.LogInformation("Lobby stopped {0}", lobby_id);

        foreach (var node in Lobbies.GetChildren())
        {
            if (node.Name == lobby_id)
            {
                Lobbies.RemoveChild(node);
                node.QueueFree();
            }
        }
    }
}
