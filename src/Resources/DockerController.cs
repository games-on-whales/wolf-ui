
using Docker.DotNet;
using Docker.DotNet.Models;
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

[GlobalClass]
public partial class DockerController : Resource
{
	DockerClient client;
	// Called when the node enters the scene tree for the first time.
	public DockerController()
	{
		client = new DockerClientConfiguration(
			new Uri("unix:///var/run/docker.sock"))
			.CreateClient();
	}

    public async Task<bool> ImageExists(string Image, string Tag = "latest")
    {
        var image = await ListImage(Image, Tag);
        if(image == null)
            return false;
        return true;
    }

    private void PullProgressCallback(JSONMessage msg, ProgressBar progressBar = null, Button button = null)
    {
        GD.Print(msg.Aux);
        if(msg.Error != null)
            GD.Print($"{msg.Error} : {msg.ErrorMessage}");
        if(msg.ProgressMessage != null)
            GD.Print(msg.ProgressMessage);
        if(msg.Progress != null)
        {
            if(msg.Progress.Total != 0)
            {
                if(progressBar != null)
                    progressBar.Value = 100 * (float)msg.Progress.Current / (float)msg.Progress.Total;
            }
        }
        if(msg.Status != null)
        {
            //GD.Print(msg.Status);
            if(msg.Status.Contains("Downloaded") || msg.Status.Contains("Image is up to date"))
            {
                progressBar?.Hide();

                if(button != null)
                    button.Disabled = false;
            }
        }
    }

    public async Task PullImage(string Image, string Tag, ProgressBar progressBar, Button appButton)
    {
        appButton.Disabled = true;
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
        return await client.Images.ListImagesAsync(
            new ImagesListParameters()
        );
    }

    public async Task<ImagesListResponse> ListImage(string Name, string Tag)
    {
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
		return await client.Containers.ListContainersAsync(
			new ContainersListParameters()
		);
	}

	public async Task<ContainerListResponse> GetContainerInfo(string Name)
	{
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

	public async void StartNewMapServer(Dictionary<string, string> lables, params string[] Envs)
	{
		string sql_user = System.Environment.GetEnvironmentVariable("SQL_USER");
		string sql_pw = System.Environment.GetEnvironmentVariable("SQL_PW");
		List<string> env = new()
        {
            $"SQL_USER={sql_user}",
            $"SQL_PW={sql_pw}"
        };
		foreach(var e in Envs)
		{
			env.Add(e);
		}
		var response = await client.Containers.CreateContainerAsync(new CreateContainerParameters()
		{
			Image = "mmoserver_masterserver",
			HostConfig = new HostConfig()
			{
				AutoRemove = true,
				NetworkMode = "backend"
			},
			Env = env,
			Labels = lables,
			//Volumes = new Dictionary<string, EmptyStruct>(){ {"", new() }}
		});

		await client.Containers.StartContainerAsync(
			response.ID,
			new ContainerStartParameters()
		);

	}

	private async void ReactToDockerEvent(Message msg)
	{
        var actor = msg.Actor;
        var attribute = actor.Attributes;
        string name = attribute["name"];
        var info = await GetContainerInfo($"/{name}");
        if(info == null)
            return;
        
        GD.Print($"{msg.Actor.ID} {msg.Action} Status: {info.Status}");
	}

    public void ListenToDockerStream()
	{
		ThreadPool.QueueUserWorkItem(new(async obj =>
        {
			Progress<Message> progress = new(ReactToDockerEvent);
			await client.System.MonitorEventsAsync(new ContainerEventsParameters(), progress, CancellationToken.None);
        }));
	}

	[Signal]
	public delegate void DockerEventEventHandler(string from, string action, string guid, string name);

}
