using System.Text.Json;
using Godot;

namespace RSG.Nonogram;

public abstract partial class Display
{
	public readonly record struct TilePosition(Vector2I Position)
	{
		public static implicit operator TilePosition(Vector2I position) => new(position);
		public static implicit operator Vector2I(TilePosition tile) => tile.Position;
	}
	public readonly record struct HintPosition(Side Side, int Index)
	{
		public static IEnumerable<HintPosition> AsRange(int length, int start = 0) => Range(start, count: length)
			.SelectMany(i => Convert(i));
		public static IEnumerable<HintPosition> Convert(OneOf<Vector2I, int> value) => [
			new(Side.Row, value.Match(position => position.X, index => index)),
			new(Side.Column, value.Match(position => position.Y, index => index))
		];

		public readonly (HorizontalAlignment, VerticalAlignment) Alignment() => (
			Side switch
			{
				Side.Row => HorizontalAlignment.Right,
				Side.Column => HorizontalAlignment.Center,
				_ => HorizontalAlignment.Fill
			},
			Side switch
			{
				Side.Row => VerticalAlignment.Center,
				Side.Column => VerticalAlignment.Bottom,
				_ => VerticalAlignment.Fill
			}
		);
	}

	public abstract record Data
	{
		public readonly record struct Empty(int Size);
		public static class PropertyNames
		{
			public const string Tiles = "Tiles", Name = "Name", Position = "Position", Value = "Value", TimeTaken = "Time Taken";
		}
		public static string ReadName(JsonElement root)
		{
			if (!root.TryGetProperty(PropertyNames.Name, out JsonElement nameProp))
			{
				return DefaultName;
			}
			return nameProp.GetString() ?? DefaultName;
		}
		public static Dictionary<Vector2I, TileMode> ReadTiles(JsonElement tilesProp)
		{
			Dictionary<Vector2I, TileMode> tiles = [];
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
				tiles[position] = valueProp.TryGetInt32(out int value) ? value.ToTileMode() : TileMode.Clear;
			}
			return tiles;
		}

		public const string DefaultName = "Puzzle";
		public const int DefaultSize = 15;
		public virtual string Name { get; set; } = DefaultName;
		public IImmutableDictionary<Vector2I, TileMode> States => Tiles.ToImmutableDictionary();
		public IEnumerable<HintPosition> HintPositions => Tiles.Keys.SelectMany(
			key => HintPosition.Convert(key)
		);
		public Dictionary<Vector2I, TileMode> Tiles { protected get; init; } = (Vector2I.One * DefaultSize)
			.GridRange()
			.ToDictionary(elementSelector: _ => TileMode.Clear);
		public virtual int Size => (int)Mathf.Sqrt(Tiles.Count);
		public Data(int size = DefaultSize)
		{
			Tiles = (Vector2I.One * size).GridRange().ToDictionary(elementSelector: _ => TileMode.Clear);
		}
		public Data(string name, Func<Vector2I, bool> selector, int size)
		{
			Name = name;
			Tiles = (Vector2I.One * size).GridRange().ToDictionary(
				elementSelector: position => selector(position) ? TileMode.Filled : TileMode.Clear
			);
		}
		public Data(Dictionary<Vector2I, Tile> tiles)
		{
			Tiles = tiles.ToDictionary(elementSelector: pair => pair.Value.Button.Text.FromText());
		}
		public bool Matches(Data expected)
		{
			foreach ((Vector2I position, TileMode state) in States)
			{
				if (!expected.Tiles.TryGetValue(position, out TileMode tile)) return false;
				if (tile is not TileMode.Filled) continue;
				if (tile != state) return false;
			}
			return true;
		}
	}

	public enum TileMode : byte { NULL = 0, Clear = 1, Filled = 2, Blocked = 3 }
	public enum Side { Row, Column }
}
