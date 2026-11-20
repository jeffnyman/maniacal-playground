// Save round trip: plays a release to the driveway, saves it in both save versions Maniacal
// writes, loads each save into a fresh game and checks it is the same game, tries a save on the
// wrong release and a damaged one, and saves from the game's own save screen into a folder.
//
//   dotnet run --project SaveRoundTrip -- <game folder> <output folder> [another release's folder]

using Maniacal.Scumm;
using Maniacal.Scumm.Game;
using Maniacal.Scumm.Graphics;
using Maniacal.Scumm.Saves;
using Maniacal.Scumm.Storage;

if (args.Length is < 2 or > 3)
{
    Console.Error.WriteLine("usage: SaveRoundTrip <game folder> <output folder> [another release's folder]");
    return 2;
}

var game = ManiacMansion.Open(args[0]);
string output = Directory.CreateDirectory(args[1]).FullName;
Console.WriteLine(game);

// to the driveway, the kids picked by the objects on screen
var world = game.Play();
world.Boot();
ToTheDriveway(world);
Console.WriteLine($"  played to room {world.CurrentRoom?.Number}: {Actors(world)}");

// 1. a save in each version, loaded into a fresh game; both games then go on 120 steps, the
// loaded one by way of the save room, as a load from ScummVM's launcher goes
int failures = 0;
var saves = new[] { SaveFile.CompatibleVersion, SaveFile.CurrentVersion }
    .Select(version => (Version: version, Bytes: world.Save("driveway", new DateTime(2026, 11, 1, 12, 0, 0), version)))
    .ToList();
string savedAs = Actors(world);
Steps(world, 120);
Console.WriteLine($"  saved with {savedAs}; 120 steps on, {Actors(world)}");
foreach (var (version, bytes) in saves)
{
    File.WriteAllBytes(Path.Combine(output, $"driveway-{version}.s01"), bytes);

    var loaded = game.Play();
    var info = loaded.Load(bytes);
    Steps(loaded, 120);
    bool same = loaded.CurrentRoom?.Number == world.CurrentRoom?.Number && Actors(loaded) == Actors(world);
    failures += same ? 0 : 1;
    Console.WriteLine($"  version {version}: {bytes.Length} bytes; loaded \"{info.Name}\", version {info.Version}, {info.PlayTime} played, saved {info.Saved:yyyy-MM-dd HH:mm}; room {loaded.CurrentRoom?.Number}, {Actors(loaded)} {(same ? "(same)" : "(DIFFERENT)")}");
}

// 2. a version no one writes
try
{
    world.Save("driveway", DateTime.Now, 99);
    Console.WriteLine("  version 99: saved (unexpected)");
}
catch (Exception ex)
{
    Console.WriteLine($"  version 99: {ex.GetType().Name}: {ex.Message}");
}

// 3. a damaged save, and a save of this release on another
var good = world.Save("driveway", DateTime.Now);
foreach (var (what, bytes) in new[] { ("a damaged save", good[..(good.Length / 2)]), ("not a save at all", new byte[100]) })
{
    try
    {
        game.Play().Load(bytes);
        Console.WriteLine($"  {what}: loaded (unexpected)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {what}: {ex.GetType().Name}: {ex.Message}");
    }
}

if (args.Length == 3)
{
    var other = ManiacMansion.Open(args[2]);
    try
    {
        var wrong = other.Play();
        var info = wrong.Load(good);
        Steps(wrong, 120);
        Console.WriteLine($"  on {other}: loaded \"{info.Name}\" (no error); room {wrong.CurrentRoom?.Number}, {Actors(wrong)}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  on {other}: {ex.GetType().Name}: {ex.Message}");
    }
}

// 4. the game's own save screen, saving into a folder: what lands there, under what name
string store = Directory.CreateDirectory(Path.Combine(output, "store")).FullName;
world.SaveStore = new FileSaveStore(store);
world.SaveTarget = "maniac";
// F5 does nothing while a cutscene runs (the kids talk on the driveway first), so it is pressed
// again until the save screen comes
for (int i = 0; i < 3000 && world.CurrentRoom?.Number != 50; i++)
{
    if (i % 50 == 0)
    {
        world.PressKey(GameWorld.KeyF1 + 4);
    }

    world.Step();
}

Steps(world, 30);
Shot(world, "save-screen.png");
var buttons = world.ObjectsHere().Where(o => o.Touchable && o.ScreenY is not null).OrderBy(o => o.Y).ToList();
Console.WriteLine($"  save screen (room {world.CurrentRoom?.Number}): {buttons.Count} objects to click, {string.Join(", ", buttons.Select(b => $"{b.Number} at ({b.ScreenX}, {b.ScreenY}) {b.Width}x{b.Height}"))}");
if (buttons.Count > 0)
{
    world.ClickObject(buttons[0].Number);
    Steps(world, 60);
    Shot(world, "save-screen-after.png");
    var next = world.ObjectsHere().Where(o => o.Touchable && o.ScreenY is not null).OrderBy(o => o.Y).ToList();
    Console.WriteLine($"  after the first: room {world.CurrentRoom?.Number}, {next.Count} objects to click");
}

Console.WriteLine($"  in the save folder: {string.Join(", ", Directory.GetFiles(store).Select(Path.GetFileName))}");
Console.WriteLine(failures == 0 ? "  round trips: all the same" : $"  round trips: {failures} DIFFERENT");
return failures == 0 ? 0 : 1;

void Steps(GameWorld w, int count)
{
    for (int i = 0; i < count; i++)
    {
        w.Step();
    }
}

void Shot(GameWorld w, string name)
{
    using var file = File.Create(Path.Combine(output, name));
    PngWriter.Write(file, w.Screen(), w.Palette.Span, 2, 2);
}

static string Actors(GameWorld w) => string.Join(", ", w.ActorsHere().Select(a => $"{a.Name} ({a.X}, {a.Y})"));

// the kids picked as a player would: Syd, Bernard and START, ten steps apart once the selection
// has settled; the portraits are its 40x48 objects, left to right Dave, Syd, Michael, Wendy,
// Bernard, Razor and Jeff, with START 40x8, but on the NES 32x40 Dave, Razor, Bernard, Syd, Wendy,
// Jeff and Michael, with START 64x24. A demo has no kids to pick: the C64's menu is answered with
// the demo that plays itself, and a demo plays on to wherever it is
void ToTheDriveway(GameWorld w)
{
    bool nes = string.Equals(game.Detected.PlatformName, "nes", StringComparison.OrdinalIgnoreCase);
    var portrait = nes ? (32, 40) : (40, 48);
    var start = nes ? (64, 24) : (40, 8);
    int[] picks = nes ? [3, 2, -1] : [1, 4, -1];
    int picked = 0, inSelection = 0;
    for (int step = 0; step < 6000 && w.CurrentRoom?.Number != 44; step++)
    {
        inSelection = w.CurrentRoom?.Number == 45 ? inSelection + 1 : 0;
        if (inSelection >= 100 && inSelection % 10 == 0)
        {
            var here = w.ObjectsHere().Where(o => o.Touchable).ToList();
            if (game.IsDemo && picked == 0)
            {
                w.ClickObject(here.OrderBy(o => o.Y).First().Number);
                picked++;
            }
            else if (!game.IsDemo && picked < picks.Length)
            {
                var portraits = here.Where(o => (o.Width, o.Height) == portrait).GroupBy(o => o.X).Select(g => g.First()).OrderBy(o => o.X).ToList();
                var target = picks[picked] >= 0 ? portraits[picks[picked]] : here.First(o => (o.Width, o.Height) == start);
                w.ClickObject(target.Number);
                picked++;
            }
        }

        if (step > 330 && step % 50 == 0 && (game.IsDemo || picked == 3))
        {
            w.SkipCutscene();
        }

        w.Step();
    }

    // and the kids standing still
    for (int i = 0; i < 200; i++)
    {
        w.Step();
    }
}
