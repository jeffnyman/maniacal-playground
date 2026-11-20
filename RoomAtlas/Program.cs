// Room atlas: every room of a Maniac Mansion release as a PNG, with its mask, and an index page
// that shows them all with their objects.
//
//   dotnet run --project RoomAtlas -- <game folder> <output folder>

using System.Net;
using System.Text;
using Maniacal.Scumm;
using Maniacal.Scumm.Graphics;

if (args.Length != 2)
{
    Console.Error.WriteLine("usage: RoomAtlas <game folder> <output folder>");
    return 2;
}

var game = ManiacMansion.Open(args[0]);
var inspector = game.Inspect();
string output = Directory.CreateDirectory(args[1]).FullName;
Console.WriteLine($"{game}: writing {inspector.Rooms.Count} rooms to {output}");

var html = new StringBuilder();
html.AppendLine($$"""
    <!doctype html>
    <meta charset="utf-8">
    <title>{{WebUtility.HtmlEncode(game.Description)}}: rooms</title>
    <style>
      body { font: 14px system-ui, sans-serif; background: #111; color: #ddd; margin: 2em; }
      section { margin-bottom: 3em; }
      img { image-rendering: pixelated; max-width: 100%; border: 1px solid #333; }
      table { border-collapse: collapse; margin-top: .5em; }
      td, th { padding: 2px 10px; text-align: left; border-bottom: 1px solid #333; }
    </style>
    <h1>{{WebUtility.HtmlEncode(game.Description)}}</h1>
    <p>{{inspector.Rooms.Count}} rooms.</p>
    """);

int objects = 0;
foreach (int number in inspector.Rooms)
{
    var room = inspector.Room(number)!;
    using (var file = File.Create(Path.Combine(output, $"room-{number:D2}.png")))
    {
        PngWriter.Write(file, room.Pixels, inspector.Palette.Span, 2, 2);
    }

    using (var file = File.Create(Path.Combine(output, $"room-{number:D2}-mask.png")))
    {
        PngWriter.Write(file, room.Mask, [Rgb.Black, Rgb.White], 2, 2);
    }

    html.AppendLine($"""
        <section id="room-{number}">
          <h2>Room {number} <small>{room.Width}×{room.Height}, {room.Objects.Count} objects</small></h2>
          <img src="room-{number:D2}.png" alt="Room {number}">
          <table>
            <tr><th>Object</th><th>Name</th><th>At</th><th>Size</th><th>Walk to</th><th>Verbs</th></tr>
        """);
    foreach (var obj in room.Objects)
    {
        html.AppendLine($"""
                <tr><td>{obj.Number}</td><td>{WebUtility.HtmlEncode(obj.Name)}</td><td>{obj.X}, {obj.Y}</td><td>{obj.Width}×{obj.Height}</td><td>{obj.WalkX}, {obj.WalkY}</td><td>{WebUtility.HtmlEncode(string.Join(", ", obj.Verbs.Select(v => inspector.VerbName(v.Verb) ?? $"#{v.Verb}")))}</td></tr>
            """);
    }

    html.AppendLine("  </table>\n</section>");
    objects += room.Objects.Count;
}

File.WriteAllText(Path.Combine(output, "index.html"), html.ToString());
Console.WriteLine($"{inspector.Rooms.Count} rooms, {objects} objects; index.html written");
return 0;
