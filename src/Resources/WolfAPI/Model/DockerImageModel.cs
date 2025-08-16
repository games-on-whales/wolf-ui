using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;

namespace Resources.WolfAPI;
 /*
     {
-     "Id":"sha256:81250e61f2a5e4651d2d94eb231018f0ed87bdd5aedc089ff31d7375ca02eace",
-     "RepoTags":["ghcr.io/games-on-whales/wolf-ui:main"],
-     "RepoDigests":["ghcr.io/games-on-whales/wolf-ui@sha256:784354da1101a73d8311e5f9c41654f3f58b9bf6d50de030253af0d37b52b63b"],
-     "Parent":"",
-     "Comment":"buildkit.dockerfile.v0",
-     "Created":"2025-07-11T01:04:13.327820084Z",
-     "ContainerConfig":{
            "Hostname":"",
            "Domainname":"",
            "User":"",
            "AttachStdin":false,
            "AttachStdout":false,
            "AttachStderr":false,
            "Tty":false,
            "OpenStdin":false,
            "StdinOnce":false,
            "Env":null,
            "Cmd":null,
            "Image":"",
            "Volumes":null,
            "WorkingDir":"",
            "Entrypoint":null,
            "OnBuild":null,
            "Labels":null
            },
-     "DockerVersion":"",
-     "Author":"",
-     "Architecture":"amd64",
-     "Os":"linux",
-     "Size":1100561684,
-     "VirtualSize":1100561684,
-     "GraphDriver":{
            "Data":{
                "LowerDir":"/var/lib/docker/overlay2/0011a4c95f488fd06b27913f9e561252855543e1270bf3a0b93a6f367bee2d0b/diff:/var/lib/docker/overlay2/8cedc4c7b19c7e6eab6562300cc230a1b3670c8c947b947f09cdc7240e961456/diff:/var/lib/docker/overlay2/5f95449998c3ea0b6cf6108e5d61d756ede04c1750e2191a69ef1391855752de/diff:/var/lib/docker/overlay2/510401fb853c446daf65f507bfa4d9122af917082e8cddf790e2ae8992dae90e/diff:/var/lib/docker/overlay2/53b24f135db548762538a11839a52c46c60af90c8a6238ecab8e84dcd87ae45d/diff:/var/lib/docker/overlay2/f88485cbdcd85be83851884b2a3535c2f5c620cd3a01f44f630bc31eaadd5a6a/diff:/var/lib/docker/overlay2/cf204e758b6489d3f77bd9e17e63ba68f23f2c273c6cd2d6839e613faa5e16e7/diff:/var/lib/docker/overlay2/935beedf466686b6f059543b4c8ebd8e1bfedaa89ffef470c57398b878464b40/diff:/var/lib/docker/overlay2/98ae8ccefaf01a0014dc42a02cb675f7790eefc63fbd4dc41ff52b55dae09413/diff:/var/lib/docker/overlay2/a366c9b45e67ec8fb28e36161833ec034f00c65734364b6a7ecc1592f98b9fa5/diff:/var/lib/docker/overlay2/ff6b6360e68c299efdb45b8786034632ce261b88c376935286bd3d6483b92f50/diff:/var/lib/docker/overlay2/4a8c0cbf4c0c16667d8ec8b3ce59e61f8654eda99595a09d0ac33e183d5100d8/diff:/var/lib/docker/overlay2/880d7b09e754a70db5e30fb529330ba78eb532d981f291bb8a40a584ac5c0a8a/diff:/var/lib/docker/overlay2/06b273238fdf0e04ef26b9b2de2a61e3e620ed254a567a3dd50e988b49dfdfd6/diff:/var/lib/docker/overlay2/4c73c3c731c9c0a9be375405a5379f62f838c4aa8670bfbff01868a03a352a9c/diff:/var/lib/docker/overlay2/5b67a6677f9c90db5ca1337a4039d9562c2d8debb5adef19e69382f1c03f3fb2/diff:/var/lib/docker/overlay2/13480eafab10ebde56c50a1a6866a4c5eb3da7962589c8aaa0683cda83129a9b/diff",
                "MergedDir":"/var/lib/docker/overlay2/8d4c6855805ead9dc02348c5dd72f479b53991e027286ba4b4d631f2303c1c83/merged",
                "UpperDir":"/var/lib/docker/overlay2/8d4c6855805ead9dc02348c5dd72f479b53991e027286ba4b4d631f2303c1c83/diff",
                "WorkDir":"/var/lib/docker/overlay2/8d4c6855805ead9dc02348c5dd72f479b53991e027286ba4b4d631f2303c1c83/work"
            },
            "Name":"overlay2"
        },
     "RootFS":{
            "Type":"layers",
            "Layers":["sha256:20d82b29063e27c205973dd8db505907257755020e61227a80c3e0fa37cf9739","sha256:baf4269311c3ed6b30a8eb98992ad08dd05f3861603d1b4f48bd6a76fb0c4968","sha256:12ce99a96e82f3bc209635225be6550eabd3e21dbcca73d8739d6006c41bf56f","sha256:1cc94fff8dfc617ad7fcf60113fb229705452a2b546d3d9188f2d0c9dbc44402","sha256:9db058c2f9b9a415c8ffa48594c1d24ec600161c5265af8605213878d25ab2a6","sha256:03f9ec9283b10b95d892998eaba59d55d87f5bb1cbdb11b24379bd6757e507a7","sha256:e743f2e679b1464b68502c25d993016979c0b57a4bb23bf5fd5213260a03c891","sha256:6afea650222eb8325fa1ac9649463e61afa4eacd69affa097e4233cfe3cee324","sha256:ad2d569c70466deb1329d28a1115f6965c5dcd5636c1ff9e69d905868b3af617","sha256:2a683e4e7ad388a6ae000cc7819f89fd98aeeae4e8f87418d9891e66105bff76","sha256:6b70d750adef4a30083fc2ffcfbcda04b85b11bbc1992400f0a75ed9390813a1","sha256:baf6d78e22b5115961caed9f05a7f708579e8fced8bbe24b8603bb274fe45398","sha256:205515137272b4fff5a6897328948495ff135ae84dd2d29afff932d981f33c00","sha256:b65da93a7a2147b2c8834eb6f83eabddfd36ace8ba76fd83f715a674a3c6a996","sha256:21c0f7061e8ffa9ab431aeba23bdef8813ed00d9d06ad4454d8b663a98651fb4","sha256:63f5245383ce166bb06cc8d15ce1980fc1f7d9745427ad225cbc4131c874e414","sha256:16232c79798069921cdc66324fb0e55705449c3ad05be41ec8ae54ec421dbc6d","sha256:ccb0656f0d43c8842545ca2f0b9d011f0c92f2f63ae90e0b551165dd935aaece"]
            },
            "Metadata":{"LastTagTime":"0001-01-01T00:00:00Z"},
            "Config":{
                    "AttachStderr":false,
                    "AttachStdin":false,
                    "AttachStdout":false,
                    "Cmd":null,
                    "Domainname":"",
                    "Entrypoint":["/entrypoint.sh"],
                    "Env":["PATH=/usr/games/:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin","PUID=0","PGID=0","UMASK=000","UNAME=root","HOME=/home/retro","TZ=Europe/London","DEBIAN_FRONTEND=noninteractive","NEEDRESTART_SUSPEND=1","GAMESCOPE_VERSION=3.15.14","BUILD_ARCHITECTURE=amd64","DEB_BUILD_OPTIONS=noddebs","XDG_RUNTIME_DIR=/tmp/.X11-unix"],
                    "Hostname":"",
                    "Image":"",
                    "Labels":{
                            "org.opencontainers.image.created":"2025-07-11T01:02:39.293Z",
                            "org.opencontainers.image.description":"A UI for Wolf, the main entrypoint when starting a streaming session",
                            "org.opencontainers.image.licenses":"",
                            "org.opencontainers.image.ref.name":"ubuntu",
                            "org.opencontainers.image.revision":"79d2ea4faeefa1c131da712f7c56629c04e7c02b",
                            "org.opencontainers.image.source":"https://github.com/games-on-whales/wolf-ui",
                            "org.opencontainers.image.title":"wolf-ui",
                            "org.opencontainers.image.url":"https://github.com/games-on-whales/wolf-ui",
                            "org.opencontainers.image.version":"main"
                            },
                    "OnBuild":null,
                    "OpenStdin":false,
                    "StdinOnce":false,
                    "Tty":false,
                    "User":"",
                    "Volumes":null,
                    "WorkingDir":"/home/retro"
                    }
            }
  */

 public class DockerRootFsConfig
 {
     [JsonInclude]
     public bool? AttachStderr { get; set; }
     [JsonInclude]
     public bool? AttachStdin { get; set; }
     [JsonInclude]
     public bool? AttachStdout { get; set; }
     [JsonInclude]
     public string? Cmd { get; set; }
     [JsonInclude]
     public string? Domainname { get; set; }
     [JsonInclude]
     public List<string>? Entrypoint  { get; set; }
     [JsonInclude]
     public List<string>? Env { get; set; }
     [JsonInclude]
     public string? Hostname { get; set; }
     [JsonInclude]
     public string? Image { get; set; }
     [JsonInclude]
     public Dictionary<string, string>? Labels { get; set; }
     [JsonInclude]
     public string? OnBuild { get; set; }
     [JsonInclude]
     public bool? OpenStdin { get; set; }
     [JsonInclude]
     public bool? StdinOnce { get; set; }
     [JsonInclude]
     public bool? Tty { get; set; }
     [JsonInclude]
     public string? User { get; set; }
     [JsonInclude]
     public List<string>? Volumes { get; set; }
     [JsonInclude]
     public string? WorkingDir { get; set; }
 }

 public class DockerRootFS
 {
     [JsonInclude]
     public string? Type { get; set; }
     [JsonInclude]
     public List<string>? Layers { get; set; }
     [JsonInclude]
     public Dictionary<string, string>? Metadata { get; set; }
     [JsonInclude]
     public DockerRootFsConfig? Config { get; set; }
 }

public class DockerImageModel
{
    [JsonInclude]
    public string? Id { get; set; }
    [JsonInclude]
    public List<string>? RepoTags { get; set; }
    [JsonInclude]
    public List<string>? RepoDigests { get; set; }
    [JsonInclude]
    public string? Parent { get; set; }
    [JsonInclude]
    public string? Comment { get; set; }
    [JsonInclude]
    public DateTime Created { get; set; }
    [JsonInclude]
    public Dictionary<string, Variant>? ContainerConfig { get; set; }
    [JsonInclude]
    public string? DockerVersion { get; set; }
    [JsonInclude]
    public string? Author { get; set; }
    [JsonInclude]
    public string? Architecture { get; set; }
    [JsonInclude]
    public string? Os { get; set; }
    [JsonInclude]
    public int? Size { get; set; }
    [JsonInclude]
    public int? VirtualSize { get; set; }
    [JsonInclude]
    public Dictionary<string, Variant>? GraphDriver { get; set; }
    [JsonInclude]
    public DockerRootFS? RootFS { get; set; }
}