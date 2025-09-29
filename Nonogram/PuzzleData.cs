using static System.Text.Json.JsonSerializer;
using System.Text.Json.Serialization;
using System.Text.Json;
using Godot;

namespace RSG;

using UI.Nonogram;
using static PuzzleData;

public sealed class PuzzleManager
{
	private const string RootPath = "res://", FileType = ".json";

	public static PuzzleData? CurrentPuzzle
	{
		get => Instance.Puzzles.GetValueOrDefault(Instance.Current);
		set
		{
			if (value is null) { return; }
			AddPuzzle(value);
			Instance.Current = value.Name;
		}
	}

	private static PuzzleManager Instance => field ??= new();

	public static void LoadCurrent(Display display)
	{
		try
		{
			string json = File.ReadAllText(LoadingPath(Instance.Current));
			GD.Print(json);
			if (Deserialize<Savable>(json, options: Converter.Options) is not Savable save)
			{
				GD.Print("Unable to find puzzle");
				return;
			}
			GD.Print("loading puzzle");
			display.Load(save.Expected, save.Current);
		}
		catch (Exception error)
		{
			GD.Print(error);
		}
	}
	public static void SaveCurrent(Display display)
	{
		if (CurrentPuzzle is PuzzleData puzzle)
		{
			Save(new Savable(display, puzzle));
		}
	}
	public static void Save(OneOf<PuzzleData, Display.Data.Empty, Savable> puzzle)
	{
		puzzle.Switch(Puzzle, Empty, Savable);
		static void Empty(Display.Data.Empty empty) => Puzzle(new(empty));
		static void Savable(Savable save)
		{
			string path = LoadingPath(save.Expected.Name);
			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
			File.WriteAllText(path, contents: Serialize(save, Converter.Options));
			AddPuzzle(save.Expected);
		}
		static void Puzzle(PuzzleData data)
		{
			string path = LoadingPath(data.Name);
			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
			File.WriteAllText(path, contents: Serialize(data, Converter.Options));
			Instance.Puzzles[data.Name] = data;
		}
	}
	public static void LoadFromCode(string code, RichTextLabel validationLabel, Display display)
	{
		Code.Encode(code).Switch(LoadingError, ValidCode);
		void LoadingError(Code.ConversionError error) => validationLabel.Text = error.Message;
		void ValidCode(Code code)
		{
			PuzzleData data = code.Decode();
			Instance.Puzzles[data.Name] = data;
			validationLabel.Text = $"valid code: {data.Name}, of size: {data.Size}";
			display.Load(data);
		}
	}
	public static void AddPuzzle(PuzzleData data)
	{
		Instance.Puzzles[data.Name] = data;
	}
	public static void AddPuzzles(IEnumerable<PuzzleData> data, Display display, NonogramContainer.Menu menu)
	{
		menu.PuzzleLoader.AddPuzzles(data, display);
	}
	public static PuzzleData? Load(string name)
	{
		if (Instance.Puzzles.TryGetValue(name, out PuzzleData? puzzle))
		{
			return puzzle;
		}
		try
		{
			string json = File.ReadAllText(LoadingPath(name));
			puzzle = Deserialize<PuzzleData>(json, options: Converter.Options);
		}
		catch (Exception) { }
		return puzzle;
	}

	private static string LoadingPath(string name)
	{
		return ProjectSettings.GlobalizePath(RootPath + name + FileType);
	}
	public Dictionary<string, PuzzleData> Puzzles { private get; init; } = new()
	{
		[Display.Data.DefaultName] = new()
	};
	public string Current { get; private set; } = Display.Data.DefaultName;
	private PuzzleManager() { }
}
public sealed record PuzzleData : Display.Data
{
	public sealed class Converter : JsonConverter<PuzzleData>
	{
		public const string TilesProp = "Tiles", NameProp = "Name", PositionProp = "Position", ValueProp = "Value";

		public static JsonSerializerOptions Options { get; } = new() { WriteIndented = true, Converters = { new Converter() } };
		public override PuzzleData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			using var doc = JsonDocument.ParseValue(ref reader);
			JsonElement root = doc.RootElement, tilesProp = root.GetProperty(TilesProp);
			return new()
			{
				Name = root.GetProperty(NameProp).GetString() ?? "",
				Tiles = tilesProp.EnumerateArray().ToDictionary(
					keySelector: element => Convert(element.GetProperty(PositionProp).GetString() ?? ""),
					elementSelector: element => element.GetProperty(ValueProp).GetBoolean()
				)
			};
		}
		public override void Write(Utf8JsonWriter writer, PuzzleData value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteString(NameProp, value.Name);
			writer.WritePropertyName(TilesProp);
			writer.WriteStartArray();
			foreach ((Vector2I position, bool state) in value.Tiles)
			{
				writer.WriteStartObject();
				writer.WriteString(PositionProp, Convert(position));
				writer.WriteBoolean(ValueProp, state);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
			writer.WriteEndObject();
		}

		private static string Convert(Vector2I position) => $"({position.X},{position.Y})";
		private static Vector2I Convert(string position)
		{
			if (string.IsNullOrEmpty(position)) return Vector2I.Zero;
			return position.Trim('(', ')').Split(',').Select(int.Parse).ToList() switch
			{
				[int x, int y] => new Vector2I(x, y),
				_ => Vector2I.Zero
			};
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
		public IReadOnlyCollection<PuzzleData> Puzzles { get; init; } = [];
	}
	public sealed class SavableConverter : JsonConverter<Savable>
	{
		public const string CurrentProp = "Current", ExpectedProp = "Expected";

		public override Savable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			JsonElement root = JsonDocument.ParseValue(ref reader).RootElement;
			if (!root.TryGetProperty(ExpectedProp, out JsonElement expectedJson)) { return null; }
			if (!root.TryGetProperty(CurrentProp, out JsonElement currentJson)) { return null; }
			return new()
			{
				Expected = Deserialize(expectedJson),
				Current = Deserialize(currentJson)
			};

			static PuzzleData Deserialize(JsonElement property)
			{
				try
				{
					string json = property.GetRawText();
					if (Deserialize<PuzzleData>(json, options: Converter.Options) is PuzzleData data)
					{
						return data;
					}
				}
				catch (Exception) { }
				return new();
			}
		}
		public override void Write(Utf8JsonWriter writer, Savable value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WritePropertyName(ExpectedProp);
			Serialize(writer, value.Expected, Converter.Options);
			writer.WritePropertyName(CurrentProp);
			Serialize(writer, value.Current, Converter.Options);
			writer.WriteEndObject();
		}
	}
	public sealed class Savable
	{
		public PuzzleData Current { get; init; } = new();
		public PuzzleData Expected { get; init; } = new();
		public Savable() { }
		public Savable(Display display, PuzzleData data)
		{
			Current = new(display);
			Expected = data;
		}
	}

	public PuzzleData(Empty empty) : base(empty.Size) { }
	public PuzzleData(Display display) : base(display) { }
	public PuzzleData(int size = DefaultSize) : base(size) { }
	public override string ToString()
	{
		string tiles = string.Join(", ", Tiles.Select(pair => $"{pair.Key}: {pair.Value}"));
		return $"{Name} ({Tiles.Count} tiles : {tiles})";
	}
}
