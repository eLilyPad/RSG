using static System.Text.Json.JsonSerializer;
using System.Text.Json.Serialization;
using System.Text.Json;
using Godot;

namespace RSG.Nonogram;

using static PuzzleBuilder;

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
	public static bool IsInLines(this Vector2I position, int thickness = 1, params ReadOnlySpan<(Vector2I start, Vector2I stop)> values)
	{
		foreach ((Vector2I start, Vector2I stop) in values)
		{
			if (start == stop)
			{
				return position.DistanceTo(start) <= thickness * .5f;
			}
			Vector2
			pos = position,
			ab = stop - start,
			ap = position - start;

			float t = ap.Dot(ab) / ab.LengthSquared();
			t = Mathf.Clamp(t, 0, 1);

			Vector2 closest = start + ab * t;
			float maxDistance = thickness * 0.5f;
			if (pos.DistanceTo(closest) <= maxDistance) return true;
		}
		return false;
	}
	public static bool IsInSpiral(this Vector2I position, Vector2I center, int a, int b, int thickness = 0)
	{
		Vector2I offset = center - position;
		return Mathf.Abs(offset.Squared() - (a + b * Mathf.Atan2(offset.Y, offset.X))) <= thickness;
	}
	public static bool IsInCircle(this Vector2I position, Vector2I center, int radius, int thickness = 0)
	{
		Vector2I offset = center - position;
		int
		innerRadius = radius - thickness,
		offsetSquared = offset.Squared();
		return thickness < 0 ? In(radius) : In(radius) && !In(innerRadius);

		bool In(int size) => size * size >= offsetSquared;
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
		}
		public override void Write(Utf8JsonWriter writer, SaveData value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WritePropertyName(ExpectedProp);
			Serialize(writer, value.Expected, PuzzleData.Converter.Options);
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
	//public sealed record 

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
	public override string Name => Expected.Name;
	public override int Size => Expected.Size;
	public bool IsComplete => Matches(expected: Expected);

	public SaveData() { }
	public SaveData(SaveData save, Display display) : base(display) => Expected = save.Expected;
	public SaveData(PuzzleData expected, Display display) : base(display) => Expected = expected;
	public SaveData(Display.Data data, Display display) : base(display)
	{
		Expected = data switch
		{
			SaveData save => save.Expected,
			PuzzleData puzzle => puzzle,
			_ => throw new NotImplementedException()
		};
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
				code += state is Display.TileMode.Fill ? FillToken : BlankToken;
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
		public string Name { get; init; } = "Pack";
		public IReadOnlyCollection<PuzzleData> Puzzles { get; init; } = [];
	}

	public static explicit operator SaveData(PuzzleData puzzle) => new() { Expected = puzzle };

	public string? DialogueName { get; init; } = null;

	public PuzzleData(Empty empty) : base(empty.Size) { }
	public PuzzleData(Display display) : base(display) { }
	public PuzzleData(string name, Func<Vector2I, bool> selector, int size) : base(name, selector, size) { }
	public PuzzleData(int size = DefaultSize) : base(size) { }

	public override string ToString()
	{
		string tiles = string.Join(", ", Tiles.Select(pair => $"{pair.Key}: {pair.Value}"));
		return $"{Name} ({Tiles.Count} tiles : {tiles})";
	}

}
