using Godot;
using Resources.WolfAPI;
using Skerga.GodotNodeUtilGenerator;

namespace WolfUI;

[Tool][SceneAutoConfigure]
public partial class LobbiesContainer : VBoxContainer
{
    private static readonly ILogger<LobbiesContainer> Logger = Main.GetLogger<LobbiesContainer>();
    public override async void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            EditorMockupReady();
            return;
        }

        Hide();

        WolfApi.Singleton.LobbyCreatedEvent += AddLobby;
        WolfApi.Singleton.LobbyStoppedEvent += OnLobbyStopped;
        Lobbies.ChildEnteredTree += (_) => SetDeferred(CanvasItem.PropertyName.Visible, true);
        Lobbies.ChildExitingTree += (_) => CallDeferred(MethodName.OnChildExitingTree);

        var currLobbies = await WolfApi.GetLobbies();
        foreach (var lobby in currLobbies)
        {
            AddLobby(this, lobby);
        }
    }

    private void EditorMockupReady()
    {
        for(var i = 0; i < 10; ++i)
        {
            var node = Lobby.Create();
            node.Name = $"{i}";
            Lobbies.AddChild(node);
        }
    }

    private void OnChildExitingTree()
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
        if (lobby.MultiUser == false)
            return;

        var node = Lobby.New(lobby);
            
        Lobbies.AddChild(node);
    }
    
    private void OnLobbyStopped(object? caller, string lobbyId)
    {
        Logger.LogInformation("Lobby stopped {0}", lobbyId);

        foreach (var node in Lobbies.GetChildren())
        {
            if (node.Name == lobbyId)
            {
                Lobbies.RemoveChild(node);
                node.QueueFree();
            }
        }
    }
}
