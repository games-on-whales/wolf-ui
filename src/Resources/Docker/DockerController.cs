
using Docker.DotNet;
using Docker.DotNet.Models;
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WolfUI;

namespace WolfManagement.Resources;

[GlobalClass]
public partial class DockerController : Resource
{
    private static HashSet<string> _cachedImages = [];
    public static HashSet<string> CachedImages {
        get
        {
            return _cachedImages;
        }
    }

    private static readonly ILogger<DockerController> Logger = WolfUI.Main.GetLogger<DockerController>();

    DockerClient client;

    private static bool _isDisabled = false;

    public static bool isDisabled
    {
        get
        {
            return _isDisabled;
        }
    }
    // Called when the node enters the scene tree for the first time.
    public DockerController()
    {
        try
        {
            if(!File.Exists("/var/run/docker.sock"))
                _isDisabled = true;

            client = new DockerClientConfiguration(
                new Uri("unix:///var/run/docker.sock"))
                .CreateClient();
        }
        catch (Exception)
        {
            _isDisabled = true;
        }
    }

    public async void UpdateCachedImages()
    {
        if (isDisabled)
            return;

        var imgs = await ListImages();
        _cachedImages.Clear();
        foreach (var i in imgs)
        {
            if(i.RepoTags.Count > 0)
                _cachedImages.Add(i.RepoTags.First());
        }
    }

    public async Task<bool> ImageExists(string Image, string Tag = "latest")
    {
        if (isDisabled)
            return true;

        var image = await ListImage(Image, Tag);
        if (image == null)
            return false;
        return true;
    }

    private static void PullProgressCallback(JSONMessage msg, ProgressBar progressBar = null, Button button = null)
    {
        if (msg.Error != null)
            Logger.LogError("{0}: {1}", msg.Error, msg.ErrorMessage);
            //GD.Print($"{msg.Error} - {msg.ErrorMessage}");
        if(msg.Progress != null)
        {
            if(msg.Progress.Total != 0)
            {
                Logger.LogInformation("{0}", msg.ProgressMessage);
                //GD.Print(msg.ProgressMessage);
                if(progressBar is not null)
                    progressBar.Value = 100 * (float)msg.Progress.Current / (float)msg.Progress.Total;
            }
            return;
        }
        if(msg.Status != null)
        {
            if(msg.ProgressMessage is not null)
                Logger.LogInformation("{0}", msg.ProgressMessage);
            //GD.Print(msg.Status);
            if(msg.Status.Contains("Downloaded") || msg.Status.Contains("Image is up to date"))
            {
                progressBar?.Hide();

                if(button is not null)
                    button.Disabled = false;
            }
        }
    }

    public async Task PullImage(string Image, string Tag, ProgressBar progressBar, Button appButton)
    {
        if (isDisabled)
            return;

        if(appButton is not null)
            appButton.Disabled = true;

        if(progressBar is not null)
            progressBar.Visible = true;
        void Msgpartial(JSONMessage c) => PullProgressCallback(c, progressBar, appButton);
        Progress<JSONMessage> msg = new(Msgpartial);
        await client.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = Image,
                Tag = Tag
            },
            null,
            msg
        );
    }

    public async Task<IList<ImagesListResponse>> ListImages()
    {
        if (isDisabled)
            return [];

        return await client.Images.ListImagesAsync(
            new ImagesListParameters()
        );
    }

    public async Task<ImagesListResponse> ListImage(string Name, string Tag)
    {
        if (isDisabled)
            return null;

        var images = await client.Images.ListImagesAsync(
            new ImagesListParameters{
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    { "reference", new Dictionary<string, bool>{ { $"{Name}:{Tag}", true } } }
                }
            }
        );
        if(images.Count > 0)
            return images[0];
        return null;
    }


    public async Task<IList<ContainerListResponse>> ListContainer()
    {
        if (isDisabled)
            return [];

        return await client.Containers.ListContainersAsync(
            new ContainersListParameters()
        );
    }

    public async Task<ContainerListResponse> GetContainerInfo(string Name)
    {
        if (isDisabled)
            return null;

        var containers = await client.Containers.ListContainersAsync(
            new ContainersListParameters {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    { "name", new Dictionary<string, bool>{ { $"{Name}", true } } }
                }
            }
        );
        if(containers.Count > 0)
            return containers[0];
        return null;
    }

    private async void ReactToDockerEvent(Message msg)
    {
        var actor = msg.Actor;
        var attribute = actor.Attributes;
        string name = attribute["name"];
        var info = await GetContainerInfo($"/{name}");
        if(info == null)
            return;

        Logger.LogInformation("{0} {1} Status: {2}", msg.Actor.ID, msg. Action, info.Status);
        //GD.Print($"{msg.Actor.ID} {msg.Action} Status: {info.Status}");
    }

    public void ListenToDockerStream()
    {
        if (isDisabled)
            return;

        Task.Run(new(async () =>
        {
            Progress<Message> progress = new(ReactToDockerEvent);
            await client.System.MonitorEventsAsync(new ContainerEventsParameters(), progress, CancellationToken.None);
        }));
    }

    [Signal]
    public delegate void DockerEventEventHandler(string from, string action, string guid, string name);

}