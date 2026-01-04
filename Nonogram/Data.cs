using System.Text.Json.Serialization;
using Godot;

namespace RSG.Nonogram;

public abstract partial class Display
{
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
		public static class PropertyNames
		{
			public const string
			Tiles = "Tiles",
			Name = "Name",
			Position = "Position",
			Value = "Value",
			DialogueName = "CompletionDialogueName",
			TimeTaken = "TimeTaken";
		}
		public static Dictionary<Vector2I, TileMode> CreateTiles(int size) => (Vector2I.One * size)
			.GridRange().ToDictionary(elementSelector: _ => TileMode.Clear);

		public const string DefaultName = "Puzzle";
		public const int DefaultSize = 15;
		public virtual string Name { get; set; } = DefaultName;
		public abstract Dictionary<Vector2I, TileMode> Tiles { protected get; init; }

		public IImmutableDictionary<Vector2I, TileMode> States => Tiles.ToImmutableDictionary();
		public IEnumerable<HintPosition> HintPositions => Tiles.Keys.SelectMany(
			key => HintPosition.Convert(key)
		);
		public virtual int Size => (int)Mathf.Sqrt(Tiles.Count);

		public Data(int size = DefaultSize) { Tiles = CreateTiles(size); }
		public Data(string name, Func<Vector2I, bool> selector, int size)
		{
			Name = name;
			Tiles = (Vector2I.One * size).GridRange().ToDictionary(
				elementSelector: position => selector(position) ? TileMode.Filled : TileMode.Clear
			);
		}
		public bool Matches(Data expected)
		{
			foreach ((Vector2I position, TileMode state) in Tiles)
			{
				if (!expected.Tiles.TryGetValue(position, out TileMode tile)) return false;
				if (tile is not TileMode.Filled) continue;
				if (tile != state) return false;
			}
			return true;
		}
	}

	[JsonConverter(typeof(JsonStringEnumConverter<TileMode>))]
	public enum TileMode
	{
		NULL = 0,
		Clear = 1,
		Filled = 2,
		Blocked = 3
	}
	public enum Side { Row, Column }
}
