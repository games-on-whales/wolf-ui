ARG BASE_APP_IMAGE=ghcr.io/games-on-whales/base-app:edge

# hadolint ignore=DL3006
FROM ${BASE_APP_IMAGE}

ARG GODOT_VERSION=4.3

RUN <<_INSTALL_DOTNET
set -e

apt-get update -y
apt-get install -y dotnet-sdk-8.0 unzip

wget -O Godot.zip https://github.com/godotengine/godot-builds/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip
unzip Godot.zip -d /usr/local/bin

mv /usr/local/bin/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64 /usr/local/bin/Godot-${GODOT_VERSION}
ln -s /usr/local/bin/Godot-${GODOT_VERSION}/Godot_v${GODOT_VERSION}-stable_mono_linux.x86_64 /usr/local/bin/Godot

_INSTALL_DOTNET

WORKDIR /src
COPY ./src .
RUN <<_INSTALL_PACKAGES

mkdir ./bin
dotnet add package Docker.DotNet
dotnet add package Tomlyn
dotnet build

_INSTALL_PACKAGES

ENV PUID=0 \
    PGID=0 \
    UNAME="root"

COPY --chmod=777 scripts/startup.sh /opt/gow/startup-app.sh

ENV XDG_RUNTIME_DIR=/tmp/.X11-unix

ARG IMAGE_SOURCE
LABEL org.opencontainers.image.source=$IMAGE_SOURCE