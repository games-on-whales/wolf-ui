ARG BASE_APP_IMAGE=ghcr.io/games-on-whales/base-app:edge
ARG GODOT_VERSION=4.4.1

###############################################################################################
##################### build wolf-ui ###########################################################
###############################################################################################
# hadolint ignore=DL3006
FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS builder
ARG GODOT_VERSION

RUN <<_INSTALL_DOTNET
set -e

apt-get update -y
apt-get install -y unzip build-essential scons pkg-config libx11-dev libxcursor-dev libxinerama-dev \
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

WORKDIR /project
COPY ./src ./src
COPY ./Skerga.GodotNodeUtilGenerator ./Skerga.GodotNodeUtilGenerator
WORKDIR /project/src
RUN <<_INSTALL_PACKAGES
set -e

mkdir ./bin
Godot --headless --export-release "Linux" ./bin/wolf-ui

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

# Unprivileged system user for rootless mode (see scripts/entrypoint.sh). Fixed
# IDs so host-side ownership stays stable across image builds. No home: GOW's
# 10-setup_user.sh runs `userdel -r` on a clashing user, and Wolf mounts the
# app state folder on /home/retro anyway.
ARG WOLF_UI_UID=990
ARG WOLF_UI_GID=990
RUN groupadd --system --gid $WOLF_UI_GID wolf-ui && \
    useradd --system --uid $WOLF_UI_UID --gid $WOLF_UI_GID --no-create-home \
    --home-dir /nonexistent --shell /usr/sbin/nologin wolf-ui

COPY --from=builder /project/src/bin /usr/local/bin
COPY --chmod=777 scripts/startup.sh /opt/gow/startup-app.sh
COPY --chmod=755 scripts/entrypoint.sh /opt/gow/wolf-ui-entrypoint.sh
ENTRYPOINT ["/opt/gow/wolf-ui-entrypoint.sh"]

ENV XDG_RUNTIME_DIR=/tmp/.X11-unix

ARG IMAGE_SOURCE
LABEL org.opencontainers.image.source=$IMAGE_SOURCE
