# Maniacal playground

Five small programs that use [Maniacal](https://github.com/jeffnyman/maniacal) the way anyone
else would: through the `Maniacal.Scumm` package from nuget.org (1.0.0), its public API
only. They were written to find out what that API was like to write against before it was promised
at 1.0, and now check each release from the outside; what they found is in [NOTES.md](NOTES.md).

Each takes a folder holding a *Maniac Mansion* release (the 1987 or 1988 DOS release or one of
their demos, the Commodore 64's two disk images or its demo's one, the NES cartridge, the Atari
ST's disk image or the Amiga's two) and a folder to write into. No game files come with them.
The NES and the C64 take longer to reach the driveway: give `AttractMode` 3000 steps for the NES,
and `Recorder` 120 seconds for the C64 and 150 for the NES.

```
dotnet build
dotnet run --project RoomAtlas -- <game folder> <output folder>
dotnet run --project ScriptBrowser -- <game folder> <output folder>
dotnet run --project AttractMode -- <game folder> <output folder> [steps] [frame every]
dotnet run --project Recorder -- <game folder> <output folder> [seconds] [frame every seconds]
dotnet run --project SaveRoundTrip -- <game folder> <output folder> [another release's folder]
```

| Program | What it writes | What it uses |
|---|---|---|
| `RoomAtlas` | Every room as a PNG (twice the size) with its mask, and `index.html` showing each room with its objects: number, name, rectangle, walk-to point, verbs | `ManiacMansion.Open`, `Inspect`, `GameInspector.Rooms`, `Room`, `Palette`, `RoomPicture`, `ObjectInfo`, `PngWriter` |
| `ScriptBrowser` | Every global script decoded, one file each, every room's entry, exit and object scripts in one file per room, and `index.txt` | `GameInspector.Scripts`, `Script`, `RoomScripts`, `ScriptListing`, `ScriptLine` |
| `AttractMode` | The title, the kid selection (two kids and START clicked, found by the objects on screen) and the intro, played headless, or a demo playing itself: a frame every so many steps, and the whole run's sound as `sound.wav` | `ManiacMansion.Play`, `GameWorld` (`Boot`, `Step`, `ObjectsHere`, `ClickObject`, `Screen`, `Palette`, `ReadAudio`, `SampleRate`, `CurrentRoom`, `ActorsHere`), `WavWriter` |
| `Recorder` | A host of its own: the game run through `CreateEngine(host)` and `Tick` on a virtual clock, frames written every so many seconds and the sound as `sound.wav`, the kids picked by mouse events made from the objects on screen | `IHost`, `IGraphicsDriver`, `IInputDriver`, `IAudioDriver`, `ISaveStore`, `ILog`, `ScummEngine` (`Initialize`, `Tick`, `World`), `TickResult`, `InputEvent`, `AudioVolume` |
| `SaveRoundTrip` | Saves from the driveway in both save versions, loaded back and compared; a version no one writes, a damaged save, another release's save; the game's own save screen saving into a folder | `GameWorld.Save`, `Load`, `SaveStore`, `SaveTarget`, `SaveInfo`, `SaveFile`, `FileSaveStore`, `ObjectsHere`, `ClickObject`, `PressKey` |

The output goes where it is told; `out/` is a good place, kept out of version control by
`.gitignore`.

## Against the repository as it is now

The programs build against the release on nuget.org (the version is in `Directory.Build.props`).
To try them on what is in the repository before it is released:

```
./use-local.ps1              # packs ../maniacal's Maniacal.Scumm and builds against it
./use-local.ps1 -Released    # back to the release on nuget.org
```

The local package gets a version of its own (the repository's, `-local.` and the date and time,
say `0.4.1-local.20261102070211`), and is restored into `.local/packages`, not NuGet's own cache,
so it never stands in for a release there. Run it again after changing the repository to pack
the change; `-Repo <folder>` names a repository elsewhere. Until `-Released`, every `dotnet build`
and `dotnet run` here uses the local package.
