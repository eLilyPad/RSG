using static System.Text.Json.JsonSerializer;
using System.Text.Json.Serialization;
using System.Text.Json;
using Godot;

namespace RSG.Nonogram;

using static PuzzleData;

using LoadResult = OneOf<Display.Data, PuzzleData.Code.ConversionError, NotFound>;

public sealed class PuzzleManager
{
	public sealed record class CurrentPuzzle
	{
		public string Name { get; private set; } = Display.Data.DefaultName;
		public Display.Data Puzzle
		{
			get => Instance.Puzzles.GetValueOrDefault(Name, defaultValue: new PuzzleData());
			set
			{
				if (value is null) { return; }
				Instance.Puzzles[value.Name] = value;
				Name = value.Name;
				if (Display is null)
				{
					return;
				}
				value.Load(Display);
			}
		}
		public Display? Display { get; set; } = null;
	}
	private const string RootPath = "res://", FileType = ".json", SavePath = "Saves";

	public static CurrentPuzzle Current => field ??= new();
	private static PuzzleManager Instance => field ??= new();

	public static IList<SaveData> GetSavedPuzzles()
	{
		IList<SaveData> puzzles = [];
		string savePath = ProjectSettings.GlobalizePath(RootPath + "/" + SavePath);
		try
		{
			foreach (string path in Directory.EnumerateFiles(savePath))
			{
				string json = File.ReadAllText(path);
				if (Deserialize<SaveData>(json, options: Converter.Options) is not SaveData data)
				{
					continue;
				}
				puzzles.Add(data);
			}
		}
		catch (Exception exception)
		{
			GD.PrintErr(exception);
			return [];
		}
		return puzzles;
	}
	public static LoadResult Load(OneOf<string, Display.Data> value)
	{
		return value.Match(
			code => Code.Encode(code).Match<LoadResult>(
				error => error,
				code =>
				{
					PuzzleData data = code.Decode();
					Instance.Puzzles[data.Name] = data;
					return data;
				}
			),
			data =>
			{
				switch (data)
				{
					case PuzzleData puzzle:
						Instance.Puzzles[puzzle.Name] = puzzle;
						break;
					case SaveData save:
						Instance.Puzzles[save.Expected.Name] = save.Expected;
						break;
				}
				return data;
			}
		);
	}
	public static void Save(OneOf<PuzzleData, Display.Data.Empty, SaveData> puzzle)
	{
		puzzle.Switch(Puzzle, Empty, Savable);
		static void Empty(Display.Data.Empty empty) => Puzzle(new(empty));
		static void Savable(SaveData save)
		{
			string path = SavedPuzzlesPath(save.Expected.Name);
			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
			string contents = Serialize(save, Converter.Options);
			File.WriteAllText(path, contents);
			Instance.Puzzles[save.Expected.Name] = save.Expected;
		}
		static void Puzzle(PuzzleData data)
		{
			string path = SavedPuzzlesPath(data.Name);
			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
			File.WriteAllText(path, contents: Serialize(data, Converter.Options));
			Instance.Puzzles[data.Name] = data;
		}
	}

	private static string SavedPuzzlesPath(string name)
	{
		string path = $"{RootPath}/{SavePath}/{name}{FileType}";
		return ProjectSettings.GlobalizePath(path);
	}

	public Dictionary<string, Display.Data> Puzzles { private get; init; } = new()
	{
		[Display.Data.DefaultName] = new PuzzleData()
	};

	private PuzzleManager() { }
}
public sealed record SaveData : Display.Data
{
	public sealed class Converter : JsonConverter<SaveData>
	{
		public const string ExpectedProp = "Expected";

		public override SaveData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			JsonElement root = JsonDocument.ParseValue(ref reader).RootElement;
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
				GD.PrintErr($"Missing property in JSON: {ExpectedProp}");
				return null;
			}
			string name = ReadName(expectedProp);
			return new SaveData
			{
				Name = name,
				Tiles = ReadTiles(tilesProp),
				Expected = new() { Name = name, Tiles = ReadTiles(expectedTilesProp) }
			};

			static Dictionary<Vector2I, bool> ReadTiles(JsonElement tilesProp)
			{
				Dictionary<Vector2I, bool> tiles = [];
				foreach (JsonElement element in tilesProp.EnumerateArray())
				{
					if (!element.TryGetProperty(PropertyNames.Position, out JsonElement positionProp)
						|| !positionProp.GetString().TryParse(out Vector2I position)
					)
					{
						GD.PrintErr($"Error parsing position in JSON: {element}");
						continue;
					}
					if (!element.TryGetProperty(PropertyNames.Value, out JsonElement valueProp))
					{
						GD.PrintErr($"Error parsing value in JSON: {element}");
						continue;
					}
					tiles[position] = valueProp.GetBoolean();
				}
				return tiles;
			}
		}
		public override void Write(Utf8JsonWriter writer, SaveData value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WritePropertyName(ExpectedProp);
			Serialize(writer, value.Expected, PuzzleData.Converter.Options);
			writer.WritePropertyName(PropertyNames.Tiles);
			writer.WriteStartArray();
			foreach ((Vector2I position, bool state) in value.Tiles)
			{
				writer.WriteStartObject();
				writer.WriteString(PropertyNames.Position, $"({position.X},{position.Y})");
				writer.WriteBoolean(PropertyNames.Value, state);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
			writer.WriteEndObject();
		}
	}
	public static OneOf<SaveData, NotFound> Create(NonogramContainer.DisplayContainer displays)
	{
		return PuzzleManager.Current.Puzzle switch
		{
			PuzzleData puzzle => (OneOf<SaveData, NotFound>)new SaveData(expected: puzzle, display: displays.CurrentTabDisplay),
			SaveData save => (OneOf<SaveData, NotFound>)new SaveData(save, display: displays.CurrentTabDisplay),
			_ => new NotFound()
		};
	}

	public PuzzleData Expected { get; init; } = new();

	public SaveData() { }
	public SaveData(SaveData save, Display display) : base(display) => Expected = save.Expected;
	public SaveData(PuzzleData expected, Display display) : base(display) => Expected = expected;

	public override void Load(Display display)
	{
		display.ChangePuzzleSize(Expected.Size);
		display.WriteToTiles(
			positions: States.Keys,
			getText: position => Expected.States.GetValueOrDefault(position)
				? Display.FillText
				: Display.EmptyText
		);
		display.WriteToHints(HintPositions);
		display.WriteToTiles(
			positions: States.Keys,
			getText: position => States.GetValueOrDefault(position)
				? Display.FillText
				: Display.EmptyText
		);
	}
}
public sealed record PuzzleData : Display.Data
{
	public sealed class Converter : JsonConverter<PuzzleData>
	{
		public static JsonSerializerOptions Options { get; } = new()
		{
			WriteIndented = true,
			Converters = { new Converter(), new SaveData.Converter() }
		};
		public override PuzzleData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			using JsonDocument doc = JsonDocument.ParseValue(ref reader);
			JsonElement root = doc.RootElement;
			if (!root.TryGetProperty(PropertyNames.Tiles, out JsonElement tilesProp))
			{
				return null;
			}
			return new PuzzleData
			{
				Name = ReadName(root),
				Tiles = ReadTiles(tilesProp)
			};
		}
		public override void Write(Utf8JsonWriter writer, PuzzleData value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteString(PropertyNames.Name, value.Name);
			writer.WritePropertyName(PropertyNames.Tiles);
			writer.WriteStartArray();
			foreach ((Vector2I position, bool state) in value.Tiles)
			{
				writer.WriteStartObject();
				writer.WriteString(PropertyNames.Position, $"({position.X},{position.Y})");
				writer.WriteBoolean(PropertyNames.Value, state);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
			writer.WriteEndObject();
		}
	}
	public readonly record struct Code
	{
		public readonly record struct ConversionError
		{
			public static ConversionError MissingSizeBarrier { get; } = new(
				message: $"the size barrier '{SizeBarrier}' is missing. cannot determine size."
			);
			public static ConversionError MissingSizeBarriers { get; } = new(
				message: $"the size barrier '{SizeBarrier}' is not around the size number. cannot determine size."
			);
			public static ConversionError MissingSize { get; } = new(
				message: $"no size number is present. cannot determine size."
			);

			public string Message { get; }
			private ConversionError(string message) { Message = message; }
		}
		private const char SizeBarrier = '-', BlankToken = '_', FillToken = 'x';

		public static (string X, string Filled) Examples => (
			"10-xooooooooxoxooooooxoooxooooxoooooxooxoooooooxxooooooooxxoooooooxooxoooooxooooxoooxooooooxoxoooooooox",
			"10-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
		);

		public static implicit operator string(Code code) => code.States;
		public static OneOf<ConversionError, Code> Encode(string value)
		{
			string[] s = value.Split(SizeBarrier);

			if (s.Length is 0) return ConversionError.MissingSizeBarrier;
			if (s.Length is not 2) return ConversionError.MissingSizeBarriers;

			string widthText = s[0], states = s[1];

			if (!int.TryParse(widthText, result: out int width)) return ConversionError.MissingSize;

			return new Code { Size = width, States = states };
		}
		public static Code Encode(PuzzleData value)
		{
			string code = "";
			code += value.Size + SizeBarrier;
			foreach ((Vector2I position, bool state) in value.Tiles)
			{
				GD.Print($"State: {position}");
				code += state ? FillToken : BlankToken;
			}
			return new()
			{
				States = code
			};
		}

		public string States { get; private init; }
		public int Size { get; private init; }
		public PuzzleData Decode()
		{
			string states = States;
			int width = Size;
			return new PuzzleData { Tiles = (Vector2I.One * Size).GridRange().ToDictionary(elementSelector: IsFillToken) };
			bool IsFillToken(Vector2I position)
			{
				int index = position.Y * width + position.X;
				if (states.Length <= index) return false;
				return states[index] is FillToken;
			}
		}

	}
	public sealed record Pack
	{
		public static Pack Procedural()
		{
			return new()
			{
				Name = "Procedural",
				Puzzles = [
					new()
					{
						Name = "Border",
						Tiles = (Vector2I.One * DefaultSize).GridRange().ToDictionary(elementSelector: BorderSelector)
					},

				]
			};

			static bool BorderSelector(Vector2I position)
			{
				if (isBorder(position.X) || isBorder(position.Y))
				{
					return true;
				}
				return false;
				static bool isBorder(int value) => value is DefaultSize - 1 or DefaultSize - 2 or 0 or 1;
			}
		}
		public string Name { get; init; } = "Pack";
		public IReadOnlyCollection<PuzzleData> Puzzles { get; init; } = [];
	}

	public PuzzleData(Empty empty) : base(empty.Size) { }
	public PuzzleData(Display display) : base(display) { }
	public PuzzleData(int size = DefaultSize) : base(size) { }

	public override void Load(Display display)
	{
		GD.Print($"Loading PuzzleData into {display}");
		display.ChangePuzzleSize(Size);
		display.WriteToTiles(States.Keys, StateAsText);
		display.WriteToHints(HintPositions);
		display.Reset();
	}
	public override string ToString()
	{
		string tiles = string.Join(", ", Tiles.Select(pair => $"{pair.Key}: {pair.Value}"));
		return $"{Name} ({Tiles.Count} tiles : {tiles})";
	}

}
