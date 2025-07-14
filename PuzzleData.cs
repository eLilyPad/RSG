using static System.Text.Json.JsonSerializer;
using System.Text.Json.Serialization;
using System.Text.Json;
using Godot;

namespace RSG;

using UI.Nonogram;

public sealed record PuzzleData : Data
{
	public sealed class Converter : JsonConverter<PuzzleData>
	{
		public override PuzzleData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			using var doc = JsonDocument.ParseValue(ref reader);
			var root = doc.RootElement;

			string name = root.GetProperty("Name").GetString() ?? "";
			PuzzleData puzzle = new() { Name = name };
			var prop = root.GetProperty("Tiles");

			foreach (var element in prop.EnumerateArray())
			{
				var posArr = element.GetProperty("Position").EnumerateArray().Select(e => e.GetInt32());
				var value = element.GetProperty("Value").GetBoolean();
				var position = new Vector2I(posArr.ElementAt(0), posArr.ElementAt(1));
				puzzle.Change(position, value);
			}

			return puzzle;
		}

		public override void Write(Utf8JsonWriter writer, PuzzleData value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteString("Name", value.Name);
			writer.WritePropertyName("Tiles");
			writer.WriteStartArray();
			foreach ((Vector2I position, bool state) in value.TileStates)
			{
				writer.WriteStartObject();
				writer.WritePropertyName("Position");
				writer.WriteStartArray();
				writer.WriteNumberValue(position.X);
				writer.WriteNumberValue(position.Y);
				writer.WriteEndArray();
				writer.WriteBoolean("Value", state);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
			writer.WriteEndObject();
		}
	}
	public sealed class SaveCode : ShareableCode<PuzzleData>
	{
		private const string SizeBarrier = "-", BlankToken = "_", FillToken = "x";

		public override PuzzleData Decode(string value)
		{
			return new();
		}

		public override string Encode(PuzzleData value)
		{
			string code = "";
			code += SizeBarrier + Mathf.Sqrt(value.TileStates.Count) + SizeBarrier;

			foreach ((Vector2I _, bool state) in value.TileStates)
			{
				code += state ? FillToken : BlankToken;
			}
			return code;
		}
	}
	public sealed class PuzzleSaver : Saver<PuzzleData>
	{
		private static JsonSerializerOptions Options { get; } = new() { WriteIndented = true, Converters = { new Converter() } };
		public override OneOf<PuzzleData, NotFound> Load()
		{
			string
			path = ProjectSettings.GlobalizePath(RootPath + Name + FileType),
			json = File.Exists(path) ? File.ReadAllText(path) : "";
			return Deserialize<PuzzleData>(json, Options) is PuzzleData puzzle ? puzzle : new NotFound();
		}

		public override void Save(PuzzleData value)
		{
			string path = ProjectSettings.GlobalizePath(RootPath + Name + FileType);
			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
			File.WriteAllText(path, contents: Serialize(value, Options));
		}
		public void SavePuzzle(PuzzleData value, OptionButton options)
		{
			Save(value);
			using DirAccess? dir = DirAccess.Open(RootPath);
			Assert(dir is not null, $"Could not open directory: {RootPath}");
			options.Clear();
			IEnumerable<string> files = dir.GetFiles()
				.Where(file => file.EndsWith(value: FileType, comparisonType: StringComparison.OrdinalIgnoreCase))
				.Select(file => Path.GetFileNameWithoutExtension(file))
				.Where(file => !string.IsNullOrEmpty(file));
			int id = 0;
			foreach (string file in files)
			{
				options.AddItem(file, id++);
			}
		}
	}

	private const string RootPath = "res://", FileType = ".json";
	public string Name { get; set; } = "Puzzle";
	public void Reset()
	{
		foreach (var (position, _) in TileStates)
		{
			Change(position, clicked: false);
		}
	}
}
