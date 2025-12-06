using static System.Text.Json.JsonSerializer;
using System.Text.Json.Serialization;
using System.Text.Json;
using Godot;

namespace RSG.Nonogram;

using static PuzzleData;
using static PuzzleBuilder;

using LoadResult = OneOf<Display.Data, PuzzleData.Code.ConversionError, NotFound>;

public static class PuzzleBuilder
{
	public enum Shape { Circle, Diamond }

	public static bool IsOverNoiseThreshold(this Vector2I position, float threshold)
	{
		(int x, int y) = position;
		FastNoiseLite noise = new()
		{
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Frequency = 0.1f,
			Seed = 1
		};
		return noise.GetNoise2D(x, y) > threshold;
	}
	public static bool IsIn(this Vector2I position, Vector2I center, int radius, Shape shape, int thickness = 0)
	{
		Vector2I offset = center - position;
		return shape switch
		{
			Shape.Circle when thickness > 0 => InCircle(radius) && !InCircle(radius - thickness),
			Shape.Circle => InCircle(radius),
			Shape.Diamond when thickness > 0 => InDiamond(radius) && !InDiamond(radius - thickness),
			Shape.Diamond => InDiamond(radius),
			_ => false
		};

		bool InCircle(int size) => size * size >= offset.Squared();
		bool InDiamond(int size) => Mathf.Abs(offset.X) + Mathf.Abs(offset.Y) <= size;
	}
}

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
				Display.Load(value);
			}
		}
		public Display? Display { get; set; } = null;
	}
	private const string RootPath = "res://", FileType = ".json", SavePath = "Saves";

	public static CurrentPuzzle Current => field ??= new();
	private static PuzzleManager Instance => field ??= new();

	public static IReadOnlyList<Pack> GetPuzzlePacks() => [.. Instance.PuzzlePacks];
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

	public List<Pack> PuzzlePacks { get; } = [Pack.Procedural()];
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
	public override int Size => Expected.Size;

	public SaveData() { }
	public SaveData(SaveData save, Display display) : base(display) => Expected = save.Expected;
	public SaveData(PuzzleData expected, Display display) : base(display) => Expected = expected;
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
			const int size = DefaultSize, radius = size / 2;

			Vector2I puzzleCenter = Vector2I.One * radius;

			return new()
			{
				Name = "Procedural",
				Puzzles = [
					new("Heart Emoji", selector: HeartEmoji, size),
					new("Spiral", selector: Spiral, size),
					new("Noise", selector: Noise, size),
					new("Smiley Face", selector: SmileyEmoji, size),
					new("Grid", selector: RemainderThreeIsZero, size),
					new("Border", selector: BorderSelector, size),
				]
			};

			bool Spiral(Vector2I position)
			{
				(int x, int y) = puzzleCenter - position;
				const int lineThickness = 1, a = 4, b = 1;

				float r = Mathf.Sqrt(x * x + y * y);
				float theta = Mathf.Atan2(y, x);

				// Spiral equation
				float expectedR = a + b * theta;

				// Check how close the pixel is to the spiral curve
				return Mathf.Abs(r - expectedR) <= lineThickness;
			}
			bool HeartEmoji(Vector2I position)
			{
				const int lineThickness = 1, curveRadius = radius / 2, size = radius - 2;
				int curveHeight = puzzleCenter.Y - radius / 4;
				Vector2I
				leftCurveCenter = puzzleCenter with { X = puzzleCenter.X - curveRadius, Y = curveHeight },
				rightCurveCenter = puzzleCenter with { X = puzzleCenter.X + curveRadius, Y = curveHeight };

				return IsBottomTriangle()
					|| IsInCurve(center: rightCurveCenter)
					|| IsInCurve(center: leftCurveCenter);

				bool IsInCurve(Vector2I center) => position.Y < puzzleCenter.Y
					&& position.IsIn(center, radius: curveRadius, shape: Shape.Circle, thickness: lineThickness);
				bool IsBottomTriangle() => position.Y > puzzleCenter.Y - 1
					&& position.IsIn(center: puzzleCenter, radius: size, shape: Shape.Diamond, thickness: lineThickness);
			}
			bool SmileyEmoji(Vector2I position)
			{
				const int
				lineThickness = 1,
				eyeSize = 1,
				mouthSize = radius / 2,
				eyeHeight = radius - 2;
				Vector2I
				rightEyeCenter = new(radius + radius / 2, eyeHeight),
				leftEyeCenter = new(radius - radius / 2, eyeHeight);

				return position.IsIn(puzzleCenter, radius, Shape.Circle, thickness: lineThickness)
					|| IsEye(center: leftEyeCenter)
					|| IsEye(center: rightEyeCenter)
					|| IsMouth();

				bool IsEye(Vector2I center) => position.IsIn(center, radius: eyeSize, shape: Shape.Circle);
				bool IsMouth() => position.IsIn(puzzleCenter, radius: mouthSize, shape: Shape.Circle, thickness: lineThickness)
					&& position.Y > puzzleCenter.Y;
			}

			static bool Noise(Vector2I position) => position.IsOverNoiseThreshold(threshold: 0);
			static bool RemainderThreeIsZero(Vector2I position) => position.X % 3 == 0 || position.Y % 3 == 0;
			static bool BorderSelector(Vector2I position)
			{
				return isBorder(position.X) || isBorder(position.Y);
				static bool isBorder(int value) => value is size - 1 or size - 2 or 0 or 1;
			}
		}
		public string Name { get; init; } = "Pack";
		public IReadOnlyCollection<PuzzleData> Puzzles { get; init; } = [];
	}

	public PuzzleData(Empty empty) : base(empty.Size) { }
	public PuzzleData(Display display) : base(display) { }
	public PuzzleData(string name, Func<Vector2I, bool> selector, int size)
	{
		Name = name;
		Tiles = (Vector2I.One * size).GridRange().ToDictionary(elementSelector: selector);
	}
	public PuzzleData(int size = DefaultSize) : base(size) { }

	public override string ToString()
	{
		string tiles = string.Join(", ", Tiles.Select(pair => $"{pair.Key}: {pair.Value}"));
		return $"{Name} ({Tiles.Count} tiles : {tiles})";
	}

}
