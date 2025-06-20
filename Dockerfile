ARG BASE_APP_IMAGE=ghcr.io/games-on-whales/base-app:edge
ARG GODOT_VERSION=4.4.1

###############################################################################################
##################### build wolf-ui ###########################################################
###############################################################################################
# hadolint ignore=DL3006
FROM ubuntu:24.10 AS builder
ARG GODOT_VERSION

RUN <<_INSTALL_DOTNET
set -e

apt-get update -y
apt-get install -y dotnet-sdk-8.0 unzip build-essential scons pkg-config libx11-dev libxcursor-dev libxinerama-dev \
    libgl1-mesa-dev libglu-dev libasound2-dev libpulse-dev libfreetype6-dev libudev-dev libxi-dev \
    libxrandr-dev yasm wget libfontconfig

wget -O Godot.zip https://github.com/godotengine/godot-builds/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip
unzip Godot.zip -d /usr/local/bin

mv /usr/local/bin/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64 /usr/local/bin/Godot-${GODOT_VERSION}
ln -s /usr/local/bin/Godot-${GODOT_VERSION}/Godot_v${GODOT_VERSION}-stable_mono_linux.x86_64 /usr/local/bin/Godot

wget -O templates.tpz https://github.com/godotengine/godot-builds/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_export_templates.tpz
mkdir -p $HOME/.local/share/godot/export_templates
unzip templates.tpz -d $HOME/.local/share/godot/export_templates
mv $HOME/.local/share/godot/export_templates/templates $HOME/.local/share/godot/export_templates/${GODOT_VERSION}.stable.mono

_INSTALL_DOTNET

WORKDIR /src
COPY ./src .
RUN <<_INSTALL_PACKAGES
set -e

mkdir ./bin
dotnet add package Docker.DotNet
Godot --headless --export-release "Linux" ./bin/wolf-ui
ls -la ./bin


test -f ./bin/wolf-ui
test -d ./bin/data_Wolf-UI_linuxbsd_x86_64

_INSTALL_PACKAGES
###############################################################################################

FROM ${BASE_APP_IMAGE}

RUN <<_INSTALL_REQIREMENTS
set -e
apt-get update -y
apt-get install -y libicu-dev

_INSTALL_REQIREMENTS

ENV PUID=0 \
    PGID=0 \
    UNAME="root"

COPY --from=builder /src/bin /usr/local/bin
COPY --chmod=777 scripts/startup.sh /opt/gow/startup-app.sh

ENV XDG_RUNTIME_DIR=/tmp/.X11-unix

ARG IMAGE_SOURCE
LABEL org.opencontainers.image.source=$IMAGE_SOURCE