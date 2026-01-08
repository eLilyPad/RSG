using static System.Text.Json.JsonSerializer;
using System.Text.Json.Serialization;
using System.Text.Json;
using Godot;

namespace RSG.Nonogram;

using Mode = Display.TileMode;

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
			Dictionary<Vector2I, Mode>? tiles = null;
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
						tiles = Deserialize<Dictionary<Vector2I, Mode>>(ref reader, options);
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
	internal sealed class AutoCompleter
	{
		public required SaveData Save { private get; set; }
		public required Tile.Pool Tiles { private get; init; }
		public required Settings Settings { private get; set; }
		public void BlockCompletedLines(Vector2I position)
		{
			BlockCompletedLine(position, side: Display.Side.Row);
			BlockCompletedLine(position, side: Display.Side.Column);
		}
		private void BlockCompletedLine(Vector2I position, Display.Side side)
		{
			if (!Save.IsLineComplete(position, side)) { return; }
			foreach ((Vector2I linePosition, Mode lineMode) in Save.Tiles.InLine(position, side))
			{
				if (lineMode is Mode.Filled) continue;
				Tile tile = Tiles.GetOrCreate(linePosition);
				if (tile.Mode is Mode.Blocked) continue;
				Save.ChangeState(position: linePosition, mode: tile.Mode = Mode.Blocked);
				if (Settings.LockCompletedBlockTiles) tile.Locked = true;
			}
		}
	}
	internal sealed class UserInput
	{
		public required SaveData Save { private get; set; }
		public required Settings Settings { private get; set; }
		public required AutoCompleter Completer { private get; init; }
		public required PuzzleTimer Timer { private get; init; }
		public required Tile.Pool Tiles { private get; init; }
		public required Tile.Locker LockRules { private get; init; }

		public void GameInput(Vector2I position, PuzzleManager.IHaveEvents? eventHandler)
		{
			const Mode defaultValue = Mode.NULL;

			Mode input = Display.PressedMode;
			if (input is defaultValue) return;

			Tile tile = Tiles.GetOrCreate(position);
			IImmutableDictionary<Vector2I, Mode> expectations = Save.Expected.States, saved = Save.States;

			Assert(expectations.ContainsKey(position), $"No expected tile in the data");
			Assert(saved.ContainsKey(position), $"No current tile in the data");
			Mode expected = expectations[position], current = saved[position];
			Assert(tile.Mode == current, "tiles displayed mode is unsynchronized from data");

			input = input == current ? Mode.Clear : input;

			if (Mode.Clear.AllEqual(current, input)) return;
			if (tile.Locked) return;
			input.PlayAudio();
			Save.ChangeState(position, mode: tile.Mode = input);

			if (LockRules.ShouldLock(position)) tile.Locked = true;
			if (Settings.LineCompleteBlockRest) Completer.BlockCompletedLines(position);
			if (!Timer.Running && input is Mode.Filled) Timer.Running = true;
			if (Save.IsComplete) eventHandler?.Completed(Save);
		}
	}

	public PuzzleData Expected { get; init; } = new();
	public TimeSpan TimeTaken { get; set; } = TimeSpan.Zero;
	[JsonConverter(typeof(Vector2IDictionaryConverter<Mode>))]
	public override Dictionary<Vector2I, Mode> Tiles { protected get; init; } = CreateTiles(DefaultSize);

	public override string Name => Expected.Name;
	public override int Size => Expected.Size;
	public int Scale => Mathf.CeilToInt(Size * Size / Size);
	public bool IsComplete => CheckComplete();

	public SaveData() { }
	public SaveData(PuzzleData expected) => Expected = expected;

	public bool IsLineComplete(Vector2I position, Display.Side side)
	{
		foreach ((Vector2I linePosition, Mode lineMode) in Tiles.InLine(position, side))
		{
			if (!Expected.States.IsCorrect(position: linePosition, current: lineMode)) return false;
		}
		return true;
	}
	public bool IsCorrectlyBlocked(Vector2I position, Mode? current = null, Mode? expected = null)
	{
		Assert(Expected.States.ContainsKey(position), $"No expected tile in the data");
		Assert(States.ContainsKey(position), $"No current tile in the data");

		return (current ?? States[position]) is Mode.Blocked
			&& (expected ?? Expected.States[position]) is Mode.Clear;
	}
	public bool IsCorrectlyFilled(Vector2I position, Mode? current = null, Mode? expected = null)
	{
		Assert(Expected.States.ContainsKey(position), $"No expected tile in the data");
		Assert(States.ContainsKey(position), $"No current tile in the data");

		return Mode.Filled.AllEqual(
			expected ?? Expected.States[position],
			current ?? States[position]
		);
	}

	private void ChangeState(Vector2I position, Mode mode)
	{
		Assert(Tiles.ContainsKey(position), "given position is not already in the base dictionary");
		Tiles[position] = mode;
	}
	private bool CheckComplete()
	{
		foreach ((Vector2I position, Mode state) in Tiles)
		{
			if (!Expected.States.IsCorrect(position, state)) return false;
		}
		return true;
	}
}
