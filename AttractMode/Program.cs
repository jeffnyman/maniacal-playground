// Attract mode: plays a Maniac Mansion release headless from its title through the kid selection
// (two kids and START clicked, as a player would) and the intro, writing a frame every so many
// steps and the whole run's sound as a WAV. A demo just runs: it plays itself (the C64's once its
// menu is answered).
//
//   dotnet run --project AttractMode -- <game folder> <output folder> [steps] [frame every]

using Maniacal.Scumm;
using Maniacal.Scumm.Graphics;
using Maniacal.Scumm.Sound;

if (args.Length is < 2 or > 4)
{
    Console.Error.WriteLine("usage: AttractMode <game folder> <output folder> [steps (1500)] [frame every (30)]");
    return 2;
}

int steps = args.Length > 2 ? int.Parse(args[2]) : 1500;
int every = args.Length > 3 ? int.Parse(args[3]) : 30;

var game = ManiacMansion.Open(args[0]);
var world = game.Play();
string output = Directory.CreateDirectory(args[1]).FullName;
Console.WriteLine($"{game}: {steps} steps, a frame every {every}, into {output}");

// the kid selection's portraits have no names, and other numbers in each release, but the same
// places, so they are picked by size and order: left to right Dave, Syd, Michael, Wendy, Bernard,
// Razor and Jeff, 40x48, with START 40x8; on the NES 32x40 in another order, with START 64x24
bool nes = string.Equals(game.Detected.PlatformName, "nes", StringComparison.OrdinalIgnoreCase);
string[] order = nes
    ? ["Dave", "Razor", "Bernard", "Syd", "Wendy", "Jeff", "Michael"]
    : ["Dave", "Syd", "Michael", "Wendy", "Bernard", "Razor", "Jeff"];
var portrait = nes ? (32, 40) : (40, 48);
var start = nes ? (64, 24) : (40, 8);
var picks = new Queue<Func<int?>>();
picks.Enqueue(() => Portraits().ElementAtOrDefault(Array.IndexOf(order, "Syd")));
picks.Enqueue(() => Portraits().ElementAtOrDefault(Array.IndexOf(order, "Bernard")));
picks.Enqueue(() => world.ObjectsHere().Where(o => o.Touchable && (o.Width, o.Height) == start).Select(o => (int?)o.Number).FirstOrDefault());

var sound = new List<short>();
var rooms = new List<int>();
int frames = 0, lastPick = -10;
bool demoChosen = false;

world.Boot();
for (int step = 0; step < steps; step++)
{
    // on the kid selection screen, a pick every ten steps once it has settled
    if (!game.IsDemo && world.CurrentRoom?.Number == 45 && step >= 300 && step - lastPick >= 10 && picks.Count > 0
        && picks.Peek()() is { } pick)
    {
        world.ClickObject(pick);
        picks.Dequeue();
        lastPick = step;
    }

    // the C64 demo opens on a menu: its first line is the demo that plays itself
    if (game.IsDemo && !demoChosen && world.CurrentRoom?.Number == 45 && step >= 300)
    {
        world.ClickObject(world.ObjectsHere().Where(o => o.Touchable).OrderBy(o => o.Y).First().Number);
        demoChosen = true;
    }

    world.Step();
    sound.AddRange(world.ReadAudio());

    if (world.CurrentRoom?.Number is { } room && (rooms.Count == 0 || rooms[^1] != room))
    {
        rooms.Add(room);
    }

    if (step % every == 0)
    {
        using var file = File.Create(Path.Combine(output, $"frame-{step:D5}.png"));
        PngWriter.Write(file, world.Screen(), world.Palette.Span, 2, 2);
        frames++;
    }
}

File.WriteAllBytes(Path.Combine(output, "sound.wav"), WavWriter.Encode(sound.ToArray(), world.SampleRate, 2));
Console.WriteLine($"{frames} frames and {world.Time.TotalSeconds:F1} seconds of sound; rooms in turn: {string.Join(" ", rooms)}");
Console.WriteLine($"Actors at the end: {string.Join(", ", world.ActorsHere().Select(a => $"{a.Name} ({a.X}, {a.Y})"))}");
return 0;

// the portraits' numbers, left to right (where two objects share a place, a click finds the first)
List<int> Portraits()
    => world.ObjectsHere().Where(o => o.Touchable && (o.Width, o.Height) == portrait)
        .GroupBy(o => o.X).Select(g => g.First()).OrderBy(o => o.X).Select(o => o.Number).ToList();
