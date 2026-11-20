// Script browser: every script of a Maniac Mansion release decoded into text files, the global
// scripts one to a file and each room's entry, exit and object scripts together, with an index.
//
//   dotnet run --project ScriptBrowser -- <game folder> <output folder>

using System.Text;
using Maniacal.Scumm;
using Maniacal.Scumm.Inspection;

if (args.Length != 2)
{
    Console.Error.WriteLine("usage: ScriptBrowser <game folder> <output folder>");
    return 2;
}

var game = ManiacMansion.Open(args[0]);
var inspector = game.Inspect();
string output = Directory.CreateDirectory(args[1]).FullName;
Directory.CreateDirectory(Path.Combine(output, "global"));
Directory.CreateDirectory(Path.Combine(output, "rooms"));
Console.WriteLine($"{game}: writing scripts to {output}");

var index = new StringBuilder();
index.AppendLine(game.Description);
index.AppendLine();

int instructions = 0, unreachable = 0, errors = 0;
index.AppendLine("Global scripts");
foreach (int number in inspector.Scripts)
{
    var listing = inspector.Script(number)!;
    File.WriteAllText(Path.Combine(output, "global", $"script-{number:D3}.txt"), Format(listing));
    index.AppendLine($"  global/script-{number:D3}.txt  {Summary(listing)}");
    Count(listing);
}

int rooms = 0;
index.AppendLine();
index.AppendLine("Rooms");
foreach (int number in inspector.Rooms)
{
    var listings = inspector.RoomScripts(number)!;
    if (listings.Count == 0)
    {
        continue;
    }

    File.WriteAllText(Path.Combine(output, "rooms", $"room-{number:D2}.txt"), string.Join("\n", listings.Select(Format)));
    index.AppendLine($"  rooms/room-{number:D2}.txt  {listings.Count} scripts: {string.Join(", ", listings.Select(l => l.Title))}");
    rooms++;
    foreach (var listing in listings)
    {
        Count(listing);
    }
}

File.WriteAllText(Path.Combine(output, "index.txt"), index.ToString());
Console.WriteLine($"{inspector.Scripts.Count} global scripts, {rooms} rooms with scripts; {instructions} instructions, {unreachable} unreachable byte runs, {errors} listings stopped by an error");
return 0;

void Count(ScriptListing listing)
{
    instructions += listing.Lines.Count(l => l.Instruction is not null);
    unreachable += listing.Lines.Count(l => l.Instruction is null);
    errors += listing.Error is null ? 0 : 1;
}

static string Summary(ScriptListing listing)
    => $"{listing.Title}: {listing.Lines.Count(l => l.Instruction is not null)} instructions{(listing.Error is null ? "" : ", stopped: " + listing.Error)}";

// a listing as text: verb labels where an object's verbs start, instructions, and the bytes between
static string Format(ScriptListing listing)
{
    var text = new StringBuilder();
    text.AppendLine(listing.Title);
    foreach (var line in listing.Lines)
    {
        foreach (var verb in listing.Verbs.Where(v => v.Offset == line.Offset && line.Instruction is not null))
        {
            text.AppendLine($"  verb {verb.Verb}:");
        }

        text.AppendLine(line.Instruction is { } instruction
            ? $"    [{line.Offset:X4}] {instruction}"
            : $"    [{line.Offset:X4}] ({line.Length} bytes never reached)");
    }

    if (listing.Error is { } error)
    {
        text.AppendLine($"    stopped: {error}");
    }

    return text.ToString();
}
