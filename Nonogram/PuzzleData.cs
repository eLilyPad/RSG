using static System.Text.Json.JsonSerializer;
using System.Text.Json.Serialization;
using System.Text.Json;
using Godot;

namespace RSG.Nonogram;

using static PuzzleBuilder;
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
			//writer.Wr
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
	public bool IsComplete => Matches(expected: Expected);

	public SaveData() { }
	public SaveData(PuzzleData expected) => Expected = expected;
	public SaveData(Display.Data data, Dictionary<Vector2I, Tile> tiles) : base(tiles)
	{
		Expected = data switch
		{
			SaveData save => save.Expected,
			PuzzleData puzzle => puzzle,
			_ => throw new NotImplementedException()
		};
	}
	public bool IsLineComplete(Vector2I position, Display.Side side)
	{
		foreach ((Vector2I expectedPosition, Display.TileMode expectedMode) in Expected.States.AllInLine(position, side))
		{
			if (!Tiles.TryGetValue(expectedPosition, out Display.TileMode currentMode)) return false;
			if (expectedMode is Display.TileMode.Clear && currentMode is Display.TileMode.Blocked) continue;
			if (expectedMode != currentMode) return false;
		}
		return true;
	}
	public void ChangeState(Vector2I position, Display.TileMode mode)
	{
		Assert(Tiles.ContainsKey(position), "given position is not already in the base dictionary");
		Tiles[position] = mode;
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
			foreach ((Vector2I position, Display.TileMode state) in value.Tiles)
			{
				writer.WriteStartObject();
				writer.WriteString(PropertyNames.Position, $"({position.X},{position.Y})");
				writer.WriteNumber(PropertyNames.Value, (double)state);
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
			foreach ((Vector2I position, Display.TileMode state) in value.Tiles)
			{
				code += state is Display.TileMode.Filled ? FillToken : BlankToken;
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
			return new PuzzleData(DefaultName, IsFillToken, Size);
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
					new("Heart Emoji", selector: HeartEmoji, size) { DialogueName = Dialogue.Intro},
					new("Kitty", selector: Cat, size) { DialogueName = Dialogue.CatOnThePath},
					new("Spiral", selector: Spiral, size),
					new("Smiley Face", selector: SmileyEmoji, size),
					//new("Noise", selector: position => position.IsOverNoiseThreshold(threshold: 0), size),
					//new("Grid", selector: position => position.X % 3 == 0 || position.Y % 3 == 0, size),
					//new("Border", selector: BorderSelector, size),
				]
			};

			bool Spiral(Vector2I position)
			{
				const int lineThickness = 1, a = 4, b = 1;
				return position.IsInSpiral(center: puzzleCenter, a, b, lineThickness);
			}
			bool HeartEmoji(Vector2I position)
			{
				const int curveRadius = (radius / 2) + 1, size = radius - 2;
				int curveHeight = puzzleCenter.Y - (radius / 3) - 1;
				Vector2I
				leftCurveCenter = puzzleCenter with { Y = puzzleCenter.Y - curveRadius + 1, X = curveHeight + 2 },
				rightCurveCenter = puzzleCenter with { Y = puzzleCenter.Y + curveRadius - 1, X = curveHeight + 2 };

				return IsBottomTriangle(puzzleCenter with { X = puzzleCenter.X + 1 })
					|| IsInCurve(center: rightCurveCenter)
					|| IsInCurve(center: leftCurveCenter);

				bool IsInCurve(Vector2I center) => position.X <= puzzleCenter.X
					&& position.IsIn(center, radius: curveRadius, shape: Shape.Circle);
				bool IsBottomTriangle(Vector2I center) => position.X >= puzzleCenter.X
					&& position.IsIn(center, radius: size, shape: Shape.Diamond);
			}
			bool SmileyEmoji(Vector2I position)
			{
				const int
				lineThickness = 1,
				eyeSize = 1,
				mouthSize = radius / 2,
				eyeHeight = radius - 2;
				Vector2I
				rightEyeCenter = new(y: radius + radius / 2, x: eyeHeight),
				leftEyeCenter = new(y: radius - radius / 2, x: eyeHeight);

				return position.IsIn(puzzleCenter, radius, Shape.Circle, thickness: lineThickness)
					|| IsEye(center: leftEyeCenter)
					|| IsEye(center: rightEyeCenter)
					|| IsMouth();

				bool IsEye(Vector2I center) => position.IsIn(center, radius: eyeSize, shape: Shape.Circle);
				bool IsMouth() => position.IsIn(center: puzzleCenter, radius: mouthSize, shape: Shape.Circle, thickness: lineThickness)
					&& position.X > puzzleCenter.X;
			}
			bool Cat(Vector2I position)
			{
				const int
				mouthHeight = radius + 4,
				faceSize = radius - 1,
				eyeHeight = radius - 1;
				Vector2I
				faceCenter = puzzleCenter with { X = puzzleCenter.X + 1 },
				rightEyeCenter = new(y: radius + radius / 3, x: eyeHeight),
				leftEyeCenter = new(y: radius - radius / 3, x: eyeHeight),
				rightMouthCenter = new(y: radius + radius / 4, x: mouthHeight - 1),
				leftMouthCenter = new(y: radius - radius / 4, x: mouthHeight - 1);
				bool
				isNose = position is (8, 7),
				isEyebrow = position is (4, 5 or 9),
				isEye = position is (6, 3 or 4 or 10 or 11)
					or (7, 4 or 5 or 9 or 10),
				isMouth = position is (10, 4 or 7 or 10)
					or (11, 5 or 6 or 8 or 9),
				isEar = position.IsInLines(1,
					(new(1, 2), new(4, 2)),
					(new(1, 12), new(4, 12)),
					(new(0, 3), new(3, 6)),
					(new(0, 11), new(3, 8))
				),
				isWhisker = position is (6, 0 or 14)
					or (9, 0 or 1 or 13 or 14)
					or (7 or 11, 1 or 13)
					or (12, 0 or 14),
				isFacialFeatures = isEye || isNose || isEyebrow || isMouth,
				isFace = position.IsIn(faceCenter, faceSize, Shape.Circle)
					&& !(position is (8, 1 or 13));

				return isFace && !isFacialFeatures || isEar || isWhisker;
			}

			static bool BorderSelector(Vector2I position)
			{
				return isBorder(position.X) || isBorder(position.Y);
				static bool isBorder(int value) => value is size - 1 or size - 2 or 0 or 1;
			}
		}
		public static (string Name, IEnumerable<SaveData> Puzzles) Convert(Pack pack)
		{
			return (pack.Name, pack.Puzzles.Select(puzzle => new SaveData(expected: puzzle)));
		}

		public string Name { get; init; } = "Pack";
		public IReadOnlyCollection<PuzzleData> Puzzles { get; init; } = [];
	}

	public static explicit operator SaveData(PuzzleData puzzle) => new(expected: puzzle);

	public string? DialogueName { get; init; } = null;

	public PuzzleData(Empty empty) : base(empty.Size) { }
	public PuzzleData(Dictionary<Vector2I, Tile> tiles) : base(tiles) { }
	public PuzzleData(string name, Func<Vector2I, bool> selector, int size) : base(name, selector, size) { }
	public PuzzleData(int size = DefaultSize) : base(size) { }

	public override string ToString()
	{
		string tiles = string.Join(", ", Tiles.Select(pair => $"{pair.Key}: {pair.Value}"));
		return $"{Name} ({Tiles.Count} tiles : {tiles})";
	}

}
