ARG BASE_APP_IMAGE=ghcr.io/games-on-whales/base-app:edge

# hadolint ignore=DL3006
FROM ${BASE_APP_IMAGE}

RUN <<_INSTALL_DOTNET

apt-get update -y
apt-get install -y dotnet-sdk-8.0 unzip

wget https://github.com/godotengine/godot-builds/releases/download/4.1.2-stable/Godot_v4.1.2-stable_mono_linux_x86_64.zip
ln -s /usr/local/bin /usr/local/bin/Godot_v4.1.2-stable_mono_linux_x86_64
unzip Godot_v4.1.2-stable_mono_linux_x86_64.zip -d /usr/local/bin

rm /usr/local/bin/Godot_v4.1.2-stable_mono_linux_x86_64
ln -s /usr/local/bin/Godot_v4.1.2-stable_mono_linux.x86_64 /usr/local/bin/Godot 

_INSTALL_DOTNET

WORKDIR /src
COPY . .
RUN <<_INSTALL_PACKAGES

mkdir ./bin
dotnet add package Docker.DotNet
dotnet add package Tomlyn
dotnet build

_INSTALL_PACKAGES

COPY --chmod=777 scripts/startup.sh /opt/gow/startup-app.sh

ENV XDG_RUNTIME_DIR=/tmp/.X11-unix

ARG IMAGE_SOURCE
LABEL org.opencontainers.image.source=$IMAGE_SOURCE