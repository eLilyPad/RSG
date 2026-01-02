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
			if (reader.TokenType != JsonTokenType.StartObject)
			{
				GD.PrintErr("SaveData root is not an object");
				return null;
			}
			PuzzleData? expected = null;
			Dictionary<Vector2I, Display.TileMode>? tiles = null;
			TimeSpan timeTaken = TimeSpan.Zero;

			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject) break;

				if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();

				string prop = reader.GetString()!;
				reader.Read();

				switch (prop)
				{
					case ExpectedProp:
						expected = Deserialize<PuzzleData>(ref reader, options);
						break;
					case PropertyNames.Tiles:
						tiles = Deserialize<Dictionary<Vector2I, Display.TileMode>>(ref reader, options);
						break;
					case PropertyNames.TimeTaken:
						timeTaken = ReadTimeSpan(ref reader, options);
						break;
					default:
						reader.Skip();
						break;
				}
			}

			if (expected is null)
			{
				GD.PrintErr($"Missing property in JSON: {ExpectedProp}");
				return null;
			}
			if (tiles is null) return null;

			return new SaveData
			{
				Name = expected.Name,
				Expected = expected,
				Tiles = tiles,
				TimeTaken = timeTaken
			};
		}
		public override void Write(Utf8JsonWriter writer, SaveData value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WritePropertyName(ExpectedProp);
			Serialize(writer, value.Expected, options);
			writer.WritePropertyName(PropertyNames.TimeTaken);
			Serialize(writer, value.TimeTaken, options);
			writer.WritePropertyName(PropertyNames.Tiles);
			Serialize(writer, value.Tiles, options);
			writer.WriteEndObject();
		}
		private static TimeSpan ReadTimeSpan(ref Utf8JsonReader reader, JsonSerializerOptions options)
		{
			try
			{
				return reader.TokenType switch
				{
					JsonTokenType.String =>
						Deserialize<TimeSpan>(ref reader, options),

					JsonTokenType.Number =>
						TimeSpan.FromSeconds(reader.GetDouble()),

					_ => TimeSpan.Zero
				};
			}
			catch (Exception e)
			{
				GD.PrintErr($"Invalid TimeTaken value: {e.Message}");
				return TimeSpan.Zero;
			}
		}
	}

	public PuzzleData Expected { get; init; } = new();
	public TimeSpan TimeTaken { get; set; } = TimeSpan.Zero;
	[JsonConverter(typeof(Vector2IDictionaryConverter<Display.TileMode>))]
	public override Dictionary<Vector2I, Display.TileMode> Tiles { protected get; init; } = CreateTiles(DefaultSize);
	public override string Name => Expected.Name;
	public override int Size => Expected.Size;
	public int Scale => Mathf.CeilToInt(Size * Size / Size);
	public bool IsComplete => Matches(expected: Expected);
	public IEnumerable<Vector2I> TileKeys => (Vector2I.One * Size).GridRange();
	public IEnumerable<Display.HintPosition> HintKeys => Display.HintPosition.AsRange(Size);

	public SaveData() { }
	public SaveData(PuzzleData expected) => Expected = expected;

	public bool IsLineComplete(Vector2I position, Display.Side side)
	{
		foreach ((Vector2I expectedPosition, Display.TileMode expectedMode) in Expected.States.AllInLine(position, side))
		{
			if (!Tiles.TryGetValue(expectedPosition, out Display.TileMode currentMode)) return false;
			if (!currentMode.Matches(expectedMode)) return false;
		}
		return true;
	}

	internal void FillLines(Vector2I position)
	{
		foreach (Display.Side side in stackalloc[] { Display.Side.Row, Display.Side.Column })
		{
			GD.Print(IsLineComplete(position, side));
			if (!IsLineComplete(position, side)) { continue; }
			var line = States.AllInLine(position, side, without: Display.TileMode.Filled);
			foreach ((Vector2I coord, Display.TileMode _) in line)
			{
				ChangeState(position: coord, mode: Display.TileMode.Blocked);
			}
		}
	}
	internal void ChangeState(Vector2I position, Display.TileMode mode)
	{
		Assert(Tiles.ContainsKey(position), "given position is not already in the base dictionary");
		Tiles[position] = mode;
	}
}
