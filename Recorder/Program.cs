// Recorder: a host of its own for the Maniacal engine. It runs a Maniac Mansion release the way a
// windowed host would (CreateEngine, Initialize, Tick at the delays the engine asks for), but on a
// virtual clock with no window: frames go to PNGs, the sound to a WAV, and the input is scripted,
// the kids picked by clicking the portraits the engine's world says are on screen.
//
//   dotnet run --project Recorder -- <game folder> <output folder> [seconds] [frame every seconds]

using Maniacal.Scumm;
using Maniacal.Scumm.Graphics;
using Maniacal.Scumm.Hosting;
using Maniacal.Scumm.Input;
using Maniacal.Scumm.Sound;
using Maniacal.Scumm.Storage;

if (args.Length is < 2 or > 4)
{
    Console.Error.WriteLine("usage: Recorder <game folder> <output folder> [seconds (90)] [frame every seconds (2)]");
    return 2;
}

double seconds = args.Length > 2 ? double.Parse(args[2]) : 90;
double frameEvery = args.Length > 3 ? double.Parse(args[3]) : 2;

var game = ManiacMansion.Open(args[0]);
string output = Directory.CreateDirectory(args[1]).FullName;
var host = new RecordingHost(output, frameEvery);
using var engine = game.CreateEngine(host);
Console.WriteLine($"{game}: recording {seconds} seconds into {output}");

engine.Initialize();

// the clock is the host's own: each tick is called when the engine asked, and told how long it has been
var clock = TimeSpan.Zero;
var elapsed = TimeSpan.Zero;
// the portraits, left to right, are Dave, Syd, Michael, Wendy, Bernard, Razor and Jeff, 40x48,
// with START 40x8; on the NES Dave, Razor, Bernard, Syd, Wendy, Jeff and Michael, 32x40, with
// START 64x24. Syd and Bernard are picked
bool nes = string.Equals(game.Detected.PlatformName, "nes", StringComparison.OrdinalIgnoreCase);
var portrait = nes ? (32, 40) : (40, 48);
var start = nes ? (64, 24) : (40, 8);
var picks = new Queue<int>(nes ? [3, 2] : [1, 4]);
bool started = false;
var nextPick = TimeSpan.FromSeconds(22);
TimeSpan? inSelection = null;
while (clock < TimeSpan.FromSeconds(seconds))
{
    // on the kid selection screen (or the C64 demo's menu), once it has shown for five seconds,
    // click a portrait, then START, a second apart; the C64 demo's first line, the demo that plays itself
    inSelection = engine.World.CurrentRoom?.Number == 45 ? inSelection ?? clock : null;
    if (!started && clock >= nextPick && clock - inSelection >= TimeSpan.FromSeconds(5))
    {
        var here = engine.World.ObjectsHere().Where(o => o.Touchable).ToList();
        var portraits = here.Where(o => (o.Width, o.Height) == portrait).GroupBy(o => o.X).Select(g => g.First()).OrderBy(o => o.X).ToList();
        var target = game.IsDemo ? here.OrderBy(o => o.Y).FirstOrDefault()
            : picks.Count > 0 ? portraits.ElementAtOrDefault(picks.Dequeue())
            : here.FirstOrDefault(o => (o.Width, o.Height) == start);
        started = game.IsDemo || (picks.Count == 0 && (target.Width, target.Height) == start);
        if (target is { ScreenX: { } x, ScreenY: { } y })
        {
            host.Input.Click(x, y);
        }

        nextPick = clock + TimeSpan.FromSeconds(1);
    }

    var result = engine.Tick(elapsed);
    if (!result.Continue)
    {
        break;
    }

    // a tick that wants to run again at once is given the shortest step a host would take
    elapsed = result.Delay > TimeSpan.Zero ? result.Delay : TimeSpan.FromMilliseconds(1);
    clock += elapsed;
    host.Advance(clock);
}

host.Finish();
Console.WriteLine($"{host.Graphics.Frames} frames written, {host.Graphics.Presented} presented; {host.Audio.Seconds:F1} seconds of sound");
Console.WriteLine($"Room {engine.World.CurrentRoom?.Number}; actors: {string.Join(", ", engine.World.ActorsHere().Select(a => a.Name))}");
return 0;

/// <summary>The platform, as the engine sees it: a display, input, sound, saves and a log, with no window anywhere.</summary>
sealed class RecordingHost(string output, double frameEvery) : IHost
{
    public RecordingGraphics Graphics { get; } = new(output, frameEvery);

    public ScriptedInput Input { get; } = new();

    public RecordingAudio Audio { get; } = new();

    public ISaveStore Saves { get; } = new MemorySaveStore();

    public ILog Log { get; } = new TextWriterLog(Console.Out, LogLevel.Warning);

    // a scripted run: the same every time
    public int? RandomSeed => 1;

    IGraphicsDriver IHost.Graphics => Graphics;

    IInputDriver IHost.Input => Input;

    IAudioDriver IHost.Audio => Audio;

    /// <summary>The virtual clock has moved on: write a frame when one is due, and take the sound made since.</summary>
    public void Advance(TimeSpan clock)
    {
        Graphics.At(clock);
        Audio.At(clock);
    }

    public void Finish() => Audio.Write(Path.Combine(output, "sound.wav"));
}

/// <summary>A display that keeps the last frame and writes one as a PNG every so many seconds.</summary>
sealed class RecordingGraphics(string output, double frameEvery) : IGraphicsDriver
{
    private readonly Rgb[] _palette = new Rgb[256];
    private Surface8? _last;
    private TimeSpan _next;

    public int Frames { get; private set; }

    public int Presented { get; private set; }

    public void SetMode(int width, int height) => _last = new Surface8(width, height);

    public void SetPalette(ReadOnlySpan<Rgb> colors, int start = 0) => colors.CopyTo(_palette.AsSpan(start));

    // the engine keeps the surface it presents, so it is copied
    public void Present(Surface8 frame)
    {
        if (_last is null || _last.Width != frame.Width || _last.Height != frame.Height)
        {
            _last = new Surface8(frame.Width, frame.Height);
        }

        _last.CopyFrom(frame);
        Presented++;
    }

    public void SetTitle(string title)
    {
    }

    public void At(TimeSpan clock)
    {
        if (_last is null || clock < _next)
        {
            return;
        }

        using var file = File.Create(Path.Combine(output, $"frame-{clock.TotalSeconds:000.0}.png"));
        PngWriter.Write(file, _last, _palette, 2, 2);
        Frames++;
        _next = clock + TimeSpan.FromSeconds(frameEvery);
    }
}

/// <summary>Input from a script: clicks are queued as the moves and presses a mouse would make.</summary>
sealed class ScriptedInput : IInputDriver
{
    private readonly Queue<InputEvent> _events = new();

    public void Click(int x, int y)
    {
        _events.Enqueue(InputEvent.MouseMove(x, y));
        _events.Enqueue(InputEvent.MouseDown(MouseButton.Left, x, y));
        _events.Enqueue(InputEvent.MouseUp(MouseButton.Left, x, y));
    }

    public bool TryRead(out InputEvent inputEvent) => _events.TryDequeue(out inputEvent);
}

/// <summary>Sound pulled from the engine's source as the virtual clock passes, kept for a WAV.</summary>
sealed class RecordingAudio : IAudioDriver
{
    private readonly List<short> _samples = [];
    private IAudioSource? _source;
    private long _frames;

    public int SampleRate => 22050;

    public int Volume { get; set; } = AudioVolume.Default;

    public double Seconds => _samples.Count / 2.0 / SampleRate;

    public void Start(IAudioSource source) => _source = source;

    public void Stop() => _source = null;

    public void At(TimeSpan clock)
    {
        long due = (long)(clock.TotalSeconds * SampleRate);
        if (_source is null || due <= _frames)
        {
            return;
        }

        var buffer = new short[(due - _frames) * 2];
        _source.Read(buffer);
        AudioVolume.Apply(buffer, Volume);
        _samples.AddRange(buffer);
        _frames = due;
    }

    public void Write(string path) => File.WriteAllBytes(path, WavWriter.Encode(_samples.ToArray(), SampleRate, 2));
}
