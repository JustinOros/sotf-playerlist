# PlayerList

Hold TAB to see a list of the players connected to your Sons of the Forest game.

## Multiplayer

Client side only. Nothing gets installed on a dedicated server, and other
players do not need the mod. Works on dedicated servers and on games hosted by
a player.

## Installation

### Easy install

Close the game, open PowerShell and paste:

```powershell
irm https://raw.githubusercontent.com/JustinOros/sotf-playerlist/main/Install.ps1 | iex
```

It finds your game, installs RedLoader if needed, and installs the latest
PlayerList. Run it again any time to update.

### Manual install

#### Step 1: Install RedLoader

[RedLoader](https://github.com/ToniMacaroni/RedLoader/releases/latest) is the mod
loader for Sons of the Forest. The game cannot load any mod without it, so
install it first. You only have to do this once.

1. Download `RedLoader.zip` from the
   [latest RedLoader release](https://github.com/ToniMacaroni/RedLoader/releases/latest)
2. Extract it into your Sons of the Forest folder, the one containing
   `SonsOfTheForest.exe`, usually
   `C:\Program Files (x86)\Steam\steamapps\common\Sons Of The Forest`
3. Launch the game once and wait until you reach the main menu. The first launch
   takes a few minutes while RedLoader processes the game files
4. Check that `MODS` appears on the main menu, then quit

#### Step 2: Install PlayerList

1. Download `PlayerList.zip` from the
   [latest release](https://github.com/JustinOros/sotf-playerlist/releases/latest)
2. Extract it into the `Mods` folder inside your game folder
3. You should end up with:

```
Mods\PlayerList.dll
Mods\PlayerList\manifest.json
```

## Usage

In a multiplayer game, hold TAB. The list shows every connected player with
your own name marked `(you)`. Release TAB to hide it.

Press F1 to open the console, then use these commands:

| Command | Action |
| --- | --- |
| `playerlist` | Show the player list on screen and write it to `_RedLoader\Latest.log` |
| `playerlist key F2` | Change the key that shows the list |

The key is saved to `UserData\PlayerList.txt` in your game folder.

## Uninstall

Close the game, open PowerShell and paste:

```powershell
& ([scriptblock]::Create((irm https://raw.githubusercontent.com/JustinOros/sotf-playerlist/main/Install.ps1))) -Remove
```

Or from a clone of this repo, run `.\Install.ps1 -Remove`. This removes only
PlayerList and its settings. RedLoader and your other mods are left alone.

## Building from source

Requires the .NET 8 SDK and RedLoader installed with its game assemblies
generated.

```powershell
.\build.ps1 -Install
```

Use `-Package` to build `PlayerList.zip` for a release. Pass `-GameDir "path"` if
the game is not found automatically.

## Troubleshooting

Check `_RedLoader\Latest.log` in your game folder. PlayerList logs a line when
it loads. Running `playerlist` in the console also logs every tracked player
entity, which helps if a name is missing or wrong.