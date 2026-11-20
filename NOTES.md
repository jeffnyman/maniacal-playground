# What the API was like to use

The three programs were first written against `Maniacal.Scumm` 0.2.0-alpha, and these notes kept
what was awkward. 0.3.0-alpha answered most of it; the programs were then moved to it and made
to use what it added, with their workarounds taken out. All five now use 1.0.0.

## With 1.0.0

1.0.0 is 0.5.0-alpha with its API promised, so the programs moved to it without a change. All
five ran on all nine releases from the published package and gave the same output as with
0.5.0-alpha: the C64 demo's round trip still differs as 20 below explains, and the byte where a
damaged save is found to end early moves from run to run, the save holding the time it was made.

## With 0.5.0-alpha

4 below is answered: `GameInspector.Verbs` and `VerbName(verb)` give the names a release shows
its verbs by, and `RoomAtlas` lists them ("Open, Close, Unlock, Use, Walk to, #16" for the 1988
front door; on the NES "Open, Close, #8, Use, Go To, #16", its door still answering an Unlock it
has no verb for). It was written against the repository first, with `./use-local.ps1`, and built
from the published package unchanged once 0.5.0-alpha was out. The other programs moved to it
without a change, and all five ran on all nine releases as with 0.4.1-alpha: the C64 demo's round
trip still differs as 20 below explains, and nothing else.

## With 0.4.1-alpha

0.4.1-alpha kept 0.4.0-alpha's API, so the programs moved to it without a change. Run again on all
nine releases, they found the two C64 demo bugs fixed (15 and 16 below): its inspector lists 14
rooms, 136 objects and 48 global scripts, with no listing stopped by an error, and its F5 no
longer throws. Everything else is as it was with 0.4.0-alpha.

20. **A save made at the C64 demo's menu loads into another room**, and that is right. The
    round trip of saves got further on the C64 demo than before (it had not run there), and
    reported a difference: the demo plays itself, goes back to its menu (room 45) and waits; a
    save made there, loaded into a fresh game, goes to the title (room 33) within a few steps,
    where the game it was saved from stays at the menu. The menu waits while its tune plays
    (script 1 asks variable 8, whether a sound is running) and goes to the title after the second
    time through; a load stops every sound, as ScummVM's does, so the loaded game finds the tune
    over. ScummVM 2026.4.0, given the same save, goes to the same title, the same to the pixel.
    So a round trip can differ wherever a script waits on a sound; the program's "DIFFERENT" on
    the C64 demo is that, not a fault in the save.

## With 0.4.0-alpha

0.4.0-alpha broke code written against 0.3.0-alpha in two places, and the programs met one: the
recorder's host lost `GetMouseState` (the compiler named it) and `DeterministicTime` (which still
compiled, as a property of its own no one reads, and was taken out by hand). The records with init
properties (`DetectedGame`, `SaveInfo`) changed nothing here, as the programs only read them.

All five programs ran on all nine releases:

| Release | Rooms | Objects | Global scripts | Attract mode |
|---|---|---|---|---|
| DOS 1988 | 53 | 460 | 169 | title, kids picked, intro, driveway (45 33 49 44) |
| DOS 1987 | 52 | 450 | 173 | the same |
| Commodore 64 | 52 | 434 | 155 | the same |
| DOS 1987 demo | 17 | 169 | 102 | plays itself (33 49 1 10 31) |
| DOS 1988 demo | 15 | 138 | 100 | plays itself (33 49 1 10 31) |
| C64 demo | (threw: 15 below) | | | its menu answered, plays itself (45 1 10 31 10) |
| NES | 54 | 470 | 171 | titles, intro, kids picked, driveway (33 49 45 44), in 3000 steps |
| Atari ST | 53 | 460 | 169 | as the DOS 1988 release |
| Amiga | 53 | 460 | 169 | as the DOS 1988 release |

Round trips of saves held on every full release, the NES's included, and the game's own save
screen came up on each (on the NES, its pause menu, "Back to the Mansion" to "New game").

15. **The C64 demo's rooms and scripts threw.** `GameInspector.Rooms` and `Scripts` failed with
    "Room 4 is on disk 2, track 6, which the disks do not have": the demo's index names rooms on
    the second disk of the full game, which the demo does not come with. Five more of its rooms
    (30, 40, 42, 46, 51) are files that hold only costumes its actors wear, with leftovers where
    the room would be, and `Room` threw on them. Fixed in the library after 0.4.0-alpha: they are
    not in the lists, and `Room` answers null.
16. **F5 in the C64 demo threw from `Step`.** F5 opens the C64's save screen, script 2, which the
    demo's index puts in room 50 on that second disk. Fixed after 0.4.0-alpha: in the demo F5
    does nothing.
17. **The NES's kid selection is laid out otherwise.** Its portraits are 32 by 40, left to right
    Dave, Razor, Bernard, Syd, Wendy, Jeff and Michael, and START 64 by 24; the programs pick by
    size and order, so they now know both layouts. Which one to use is told by
    `Detected.PlatformName`, which is ScummVM's code, `nes`, not the `NES` the description shows;
    its documentation said only `dos` or `c64` (now all five codes).
18. **The C64 demo starts on a menu** (room 45, like the kid selection, but two lines of text):
    the programs answer it with its first line, the demo that plays itself.
19. **A save screen's PNG used `Palettes.ForVersion`**, which knows only the DOS and C64 colours;
    `GameWorld.Palette` gives the NES's, the Atari ST's and the Amiga's too, and is what a program
    should use.

## With 0.3.0-alpha

All three programs ran on the 1988 and 1987 DOS releases, the Commodore 64's, and both DOS demos,
without a workaround or an error:

| Release | Rooms | Objects | Global scripts | Attract mode |
|---|---|---|---|---|
| DOS 1988 | 53 | 460 | 169 | title, kids picked, intro, driveway (45 33 49 44) |
| DOS 1987 | 52 | 450 | 173 | the same |
| Commodore 64 | 52 | 434 | 155 | the same: the kids are now picked here too |
| DOS 1987 demo | 17 | 169 | 102 | plays itself (33 49 1 10 31) |
| DOS 1988 demo | 15 | 138 | 100 | plays itself (33 49 1 10 31) |

No listing stopped by a decoding error, and every release's sound came out whole.

What each note below came to:

1. **Number 0 and empty slots threw.** Fixed: the inspector answers null for an entry in room 0,
   which is how the index keeps an empty slot. The 1987 release's script 0 turned out to be a
   real script, so the rule is the entry's room, not the number. Gone from the programs: the
   count from 1, and the try and catch around each number.
2. **Dead entries looked like damage.** Fixed: entries pointing at nothing the game loads answer
   null too, and so does the 1987 release's costume 0, which holds no costume.
3. **Nothing listed the rooms, scripts or costumes.** `GameInspector.Rooms`, `Scripts` and
   `Costumes`. The programs loop over them instead of over guessed ranges.
4. **Verbs come as numbers.** Still so with 0.3.0-alpha to 0.4.1-alpha: the DOS releases' verb
   names are in their own scripts. Answered in 0.5.0-alpha (above).
5. **Picking the kids meant knowing where they are.** `GameWorld.ObjectsHere()` and
   `ClickObject`. The portraits have no names and other numbers in each release, but they are
   the 40 by 48 objects left to right and START the 40 by 8 one, so the attract mode finds them by
   size and order, the C64 included.
6. **Opening a game took four types and four namespaces.** `ManiacMansion.Open(folder)`, which
   also says what it found (`Maniac Mansion (V2, English, DOS)`) and gives `Play()` and
   `Inspect()`. The programs now begin with it, and their `using`s of Maniacal's namespaces went
   from as many as six to two or three.
7. **Reading the sound was left to the caller.** `GameWorld.ReadAudio()` and `SampleRate`. The
   attract mode's frames-due arithmetic went: one line per step now.
8. **The palette was a span.** `GameWorld.Palette` and `GameInspector.Palette`, a
   `ReadOnlyMemory<Rgb>`.

## Still rough

- **The kid selection is found by shape.** It works on every release, but "the 40 by 48 objects
  left to right" is knowledge about this game's screen, not something the API says. A program
  that wanted the kids by name would need it written down somewhere, perhaps the documentation
  of `ObjectsHere`.
- **The demos' actors have no names** (`ActorsHere` gives empty ones). That is the demos' data,
  not the API, but a program printing who is on screen shows blanks.

## A host of its own (the recorder)

The fourth program, `Recorder`, is a host written from the package's documentation alone: it
implements `IHost` (a display that writes PNGs, scripted input, an audio driver that pulls the
sound on a virtual clock, a save store and a log) and runs the game through `CreateEngine(host)`,
`Initialize` and `Tick`, as a windowed host would. It picks the kids by turning what
`engine.World.ObjectsHere()` says into mouse events. It worked the first time on all five
releases: the three full ones from the title through the kid selection and the intro to the
driveway with the kids picked, the demos playing to the lab, each with all its sound, 120 virtual
seconds in about a second.

What the documentation left to be guessed, each looked up in the engine's code afterwards. All
five are settled in the library after 0.3.0-alpha: 9 and 10 removed, 11 to 13 documented.

9. **`IHost.DeterministicTime` does nothing.** Its documentation says the engine then advances
   its clock by a fixed step; `ScummEngine` never reads it (it came with the host layer from
   another project). A host sets it, and nothing changes.
10. **`IInputDriver.GetMouseState()` is never called.** Every host must write it, but the engine
    follows the mouse by `MouseMove` events only.
11. **What `Tick`'s `elapsed` means to this engine.** It is documented as the time since the last
    tick; `ScummEngine` ignores it, running one step of the game a tick, and the delay it returns
    is what paces it. What to pass after a zero delay was a guess (it does not matter).
12. **Who applies `IAudioDriver.Volume`.** "Applied to everything the source produces" does not
    say the driver must do it (the engine does not); `AudioVolume.Apply` is there for it.
13. **Which input events the engine reads.** `MouseMove`, left and right `MouseDown`, `KeyDown`
    and `Quit`; `MouseUp` is ignored. A host has to find that out.

## Saves (the save round trip)

The fifth program, `SaveRoundTrip`, plays to the driveway, saves with `Save` in both versions
Maniacal writes (108, which ScummVM 2.7 reads, and 124), loads each into a fresh game, tries a
version no one writes, a damaged save, a file that is no save, a save on another release, and the
game's own save screen with a `FileSaveStore`.

- **Round trips hold** in every release and both versions: the name, version, time played and
  date come back, and the game loaded and the game saved, each 120 steps on, have the same room
  and the kids in the same places. (A first comparison of the loaded game 120 steps on with the
  saved one as it was found Bernard moved in the 1987 and C64 releases; he walks there anyway.)
- **Errors are clear**: version 99 is an `ArgumentOutOfRangeException` ("Saves are written in
  versions 108 to 124"); a damaged save and a file that is no save are `InvalidDataException`s.
- **The game's own save screen** saves into the `SaveStore` under `SaveTarget`: `maniac.s01` in
  the folder, from the 1987 and C64 releases' first button (the 1988 release asks for a slot
  first). F5 does nothing while a cutscene runs, so a program presses it until the save room
  (50) comes.

14. **A save from another release was loaded, and the game then failed.** A 1988 save on the
    1987 release failed while being read ("The save ends early"), but a 1987 or C64 save on the
    1988 release loaded, and the game then threw `IndexOutOfRangeException` or
    `OverflowException` while stepping. Fixed in the library after 0.3.0-alpha: a save keeps the
    MD5 of its release (ScummVM's `_gameMD5`), so `Load` refuses one made with another version or
    platform by name, before the world is touched, and any other failure in reading a save is an
    `InvalidDataException`.

## What worked well

- The inspection records were easy to use: `RoomPicture`, `ObjectInfo`, `ScriptListing` and
  `ScriptLine` say what they hold, and the documentation comments answered every question that
  came up about them.
- `PngWriter` and `WavWriter` made the output one line each.
- Headless play was straightforward: `Boot`, `Step`, `Screen`, `CurrentRoom` and `ActorsHere`.
- The host contract held up: four small classes and a loop, and the engine ran in a host it had
  never seen, `TickResult`'s delays pacing it and `ScummEngine.World` bridging to what is on
  screen.
- With 0.3.0-alpha the programs read as what they do: open, list, write.
