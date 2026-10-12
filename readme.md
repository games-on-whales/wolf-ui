# Wolf-UI
A UI for Wolf, the main entrypoint when starting a streaming session 

----
### Supported Environment Args
- WOLF_UI_ARGS 

Append arguments to the Wolf-UI launch command.
To force Wolf-UI to use the OpenGl3 backend use `WOLF_UI_ARGS=--rendering-method gl_compatibility --rendering-driver opengl3`

- WOLF_SOCKET_PATH 

Set the path where Wolf-UI will look for the socket

- LOGLEVEL

Set the loglevel, valid arguments: `NONE` `ERROR` `WARNING` `WARN` `INFORMATION` `INFO` `DEBUG`

- WOLF_UI_AUTOUPDATE

if set to `True` then Wolf-UI will ask for a pull of the latest Wolf-UI image on start. Still WIP for none stable releases

- WOLF_UI_ROOTLESS

if set to `true` then Wolf-UI runs rootless, see below. Passing `--rootless` as the container command does the same.

---
### Running rootless
By default the container starts as root, runs the GOW init scripts and then starts Wolf-UI as `UNAME` (`root`), same as before.

The image also ships an unprivileged `wolf-ui` system user (UID/GID `990`, can be changed at build time with the `WOLF_UI_UID`/`WOLF_UI_GID` build args).
Start the container as that user and set `WOLF_UI_ROOTLESS=true`: the root-only init scripts and `gosu` are skipped and Wolf-UI runs as `wolf-ui`, so all capabilities can be dropped.
In Wolf's `config.toml`:

```toml
[profiles.apps.runner]
base_create_json = '''{
      "User": "wolf-ui",
      "HostConfig": {
        "IpcMode": "host",
        "CapDrop": ["ALL"],
        "Privileged": false
      }
    }'''
env = [
    'WOLF_UI_ROOTLESS=true',
    'WOLF_SOCKET_PATH=/var/run/wolf/wolf.sock',
    'WOLF_UI_AUTOUPDATE=False',
    'LOGLEVEL=INFO'
]
```

- Device access comes from the groups the runtime grants (e.g. `GroupAdd: ["keep-groups"]` with podman); Wolf-UI can't add itself to device groups without root.
- The Wolf socket must be readable and writable by the `wolf-ui` user.
- If the home directory Wolf mounts (`/home/retro`) isn't writable by `wolf-ui`, a temporary one under `/tmp` is used instead.
- With the NVIDIA driver volume mounted on `/usr/nvidia`, the loader env vars (`LD_LIBRARY_PATH`, `__EGL_VENDOR_LIBRARY_DIRS`, ...) are pointed at it, since registering it with `ldconfig` needs root.
- Rootless mode refuses to run as root. To start as root and drop privileges instead, leave it off and set `UNAME`/`PUID`/`PGID` as usual; if those clash with the built-in `wolf-ui` user (name, UID or GID), it is removed and recreated with the IDs you asked for.

---
### Special thanks to: 
- [THOSE AWESOME GUYS](https://thoseawesomeguys.com/) for their awsome [icon pack](https://thoseawesomeguys.com/prompts/)