#!/usr/bin/env bash
# Wolf-UI entrypoint, in front of the GOW base /entrypoint.sh.
#
# By default it hands straight over to /entrypoint.sh (root-only init scripts,
# then gosu to $UNAME), exactly as before.
#
# Rootless mode is opt-in: set WOLF_UI_ROOTLESS=true (or pass --rootless as the
# container command). The container must then already be started as a
# non-root user, e.g. the image's `wolf-ui` user ("User": "wolf-ui" in the
# app's base_create_json, or `--user wolf-ui`). The GOW init and gosu, which
# both need root, are skipped and Wolf-UI runs as that user, so all
# capabilities can be dropped. Device access comes from the groups the runtime
# grants (e.g. `--group-add keep-groups`).
set -e

source /opt/gow/bash-lib/utils.sh

rootless=false
if [ "${1:-}" = "--rootless" ]; then
    rootless=true
    shift
elif [[ "${WOLF_UI_ROOTLESS,,}" =~ ^(true|1|yes)$ ]]; then
    rootless=true
fi

if [ "$rootless" != "true" ]; then
    # The image ships a `wolf-ui` system user for rootless mode. If the GOW init
    # is asked to create a user clashing with it (UNAME=wolf-ui, or its UID/GID),
    # remove ours first so 10-setup_user.sh can create it with the requested IDs.
    ui_uid=$(id -u wolf-ui 2>/dev/null || true)
    ui_gid=$(id -g wolf-ui 2>/dev/null || true)
    if [ "$(id -u)" = "0" ] && [ -n "$ui_uid" ] && [ "${UNAME:-root}" != "root" ] &&
        { [ "$UNAME" = "wolf-ui" ] || [ "${PUID:-1000}" = "$ui_uid" ] || [ "${PGID:-1000}" = "$ui_gid" ]; }; then
        gow_log "UNAME/PUID/PGID clash with the built-in wolf-ui user, removing it"
        userdel wolf-ui 2>/dev/null || true
        groupdel wolf-ui 2>/dev/null || true
    fi
    exec /entrypoint.sh "$@"
fi

if [ "$(id -u)" = "0" ]; then
    gow_log "Rootless mode needs the container started as a non-root user (e.g. --user wolf-ui)."
    gow_log "To start as root and drop privileges instead, unset WOLF_UI_ROOTLESS and set UNAME/PUID/PGID."
    exit 1
fi

# Wolf mounts the app state folder on $HOME (/home/retro), but without the GOW
# init nobody chowns it to us. Godot and fontconfig need a writable home for
# their user data and caches, so fall back to a private one.
if [ ! -w "${HOME:-/nonexistent}" ]; then
    gow_log "HOME ($HOME) isn't writable by $(id -u), using a temporary one"
    export HOME=/tmp/wolf-ui-home
    mkdir -p "$HOME"
fi

# GOW's 30-nvidia.sh registers the Wolf NVIDIA driver volume with ldconfig and
# copies its ICD files into /usr/share, which needs root. Point the loaders at
# the volume instead.
if [ -d /usr/nvidia ]; then
    gow_log "Nvidia driver volume detected, pointing the loaders at /usr/nvidia"
    export LD_LIBRARY_PATH=/usr/nvidia/lib${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}
    export __EGL_VENDOR_LIBRARY_DIRS=${__EGL_VENDOR_LIBRARY_DIRS:-/usr/nvidia/share/glvnd/egl_vendor.d:/usr/share/glvnd/egl_vendor.d}
    export __EGL_EXTERNAL_PLATFORM_CONFIG_DIRS=${__EGL_EXTERNAL_PLATFORM_CONFIG_DIRS:-/usr/nvidia/share/egl/egl_external_platform.d:/usr/share/egl/egl_external_platform.d}
    export GBM_BACKENDS_PATH=${GBM_BACKENDS_PATH:-/usr/nvidia/lib/gbm}
    export VK_DRIVER_FILES=${VK_DRIVER_FILES:-/usr/nvidia/share/vulkan/icd.d/nvidia_icd.json}
fi

gow_log "Running Wolf-UI rootless as $(id -un 2>/dev/null || id -u)"
if [ $# -gt 0 ]; then
    exec /bin/bash -c "$*"
fi
exec /opt/gow/startup.sh
