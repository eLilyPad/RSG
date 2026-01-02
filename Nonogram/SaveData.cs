using static System.Text.Json.JsonSerializer;
using System.Text.Json.Serialization;
using System.Text.Json;
using Godot;

namespace RSG.Nonogram;

public sealed record SaveData : Display.Data
{
	public sealed class Converter : JsonConverter<SaveData>
	{
		public const string ExpectedProp = "Expected";

		public override SaveData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			using JsonDocument doc = JsonDocument.ParseValue(ref reader);
			JsonElement root = doc.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
			{
				GD.PrintErr("SaveData root is not an object");
				return null;
			}
			if (!root.TryGetProperty(ExpectedProp, out JsonElement expectedProp))
			{
				GD.PrintErr($"Missing property in JSON: {ExpectedProp}");
				return null;
			}
			if (!root.TryGetProperty(PropertyNames.Tiles, out JsonElement tilesProp))
			{
				return null;
			}
			if (!expectedProp.TryGetProperty(PropertyNames.Tiles, out JsonElement expectedTilesProp))
			{
				GD.PrintErr($"Missing property in JSON: Expected.{PropertyNames.Tiles}");
				return null;
			}

			TimeSpan timeTaken = TimeSpan.Zero;
			if (root.TryGetProperty(PropertyNames.TimeTaken, out JsonElement timeProp))
			{
				try
				{
					timeTaken = timeProp.ValueKind switch
					{
						JsonValueKind.String => timeProp.Deserialize<TimeSpan>(options),
						JsonValueKind.Number => TimeSpan.FromSeconds(timeProp.GetDouble()),
						_ => TimeSpan.Zero
					};
				}
				catch (Exception e)
				{
					GD.PrintErr($"Invalid TimeTaken value: {e.Message}");
				}
			}

			string name = ReadName(expectedProp);
			return new SaveData
			{
				Name = name,
				TimeTaken = timeTaken,
				Tiles = ReadTiles(tilesProp),
				Expected = new() { Name = name, Tiles = ReadTiles(expectedTilesProp) }
			};
		}
		public override void Write(Utf8JsonWriter writer, SaveData value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WritePropertyName(ExpectedProp);
			Serialize(writer, value.Expected, PuzzleData.Converter.Options);
			writer.WritePropertyName(PropertyNames.TimeTaken);
			Serialize(writer, value.TimeTaken, options);
			writer.WritePropertyName(PropertyNames.Tiles);
			writer.WriteStartArray();
			foreach ((Vector2I position, Display.TileMode state) in value.Tiles)
			{
				writer.WriteStartObject();
				writer.WriteString(PropertyNames.Position, $"({position.X},{position.Y})");
				writer.WriteNumber(PropertyNames.Value, state.ToDouble());
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
			writer.WriteEndObject();
		}
	}

	public PuzzleData Expected { get; init; } = new();
	public TimeSpan TimeTaken { get; set; } = TimeSpan.Zero;
	public override string Name => Expected.Name;
	public override int Size => Expected.Size;
	public int Scale => Mathf.CeilToInt(Size * Size / Size);
	public bool IsComplete => Matches(expected: Expected);
	public IEnumerable<Vector2I> TileKeys => (Vector2I.One * Size).GridRange();
	public IEnumerable<Display.HintPosition> HintKeys => Display.HintPosition.AsRange(Size);

	public SaveData() { }
	public SaveData(PuzzleData expected) => Expected = expected;

	public bool Matches(Vector2I position, Display.TileMode? current = null, Display.TileMode? expected = null)
	{
		expected ??= Expected.States.GetValueOrDefault(position, defaultValue: Display.TileMode.NULL);
		current ??= States.GetValueOrDefault(position, defaultValue: Display.TileMode.NULL);
		return expected == current;
	}
	public bool IsLineComplete(Vector2I position, Display.Side side)
	{
		foreach ((Vector2I expectedPosition, Display.TileMode expectedMode) in Expected.States.AllInLine(position, side))
		{
			if (!Tiles.TryGetValue(expectedPosition, out Display.TileMode currentMode)) return false;
			if (expectedMode is Display.TileMode.Clear && currentMode is Display.TileMode.Blocked) continue;
			if (!Matches(expectedPosition, currentMode, expectedMode)) return false;
		}
		return true;
	}
	internal void ChangeState(Vector2I position, Display.TileMode mode)
	{
		Assert(Tiles.ContainsKey(position), "given position is not already in the base dictionary");
		Tiles[position] = mode;
	}
}
