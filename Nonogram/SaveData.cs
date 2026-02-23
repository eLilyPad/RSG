using System.Text.Json.Serialization;
using Godot;
using RSG.UI;

namespace RSG.Nonogram;

using Mode = Display.TileMode;

public interface IPuzzleHints
{
	public int PuzzleSize => RowHints.Count;
	public IReadOnlyList<IReadOnlyList<int>> RowHints { get; }
	public IReadOnlyList<IReadOnlyList<int>> ColumnHints { get; }
}

public sealed partial record SaveData : Display.Data, IPuzzleHints
{
	public PuzzleData Expected { get; init; } = new();
	public TimeSpan TimeTaken { get; set; } = TimeSpan.Zero;
	[JsonConverter(typeof(Vector2IDictionaryConverter<Mode>))]
	public override Dictionary<Vector2I, Mode> Tiles { protected get; init; } = CreateTiles(DefaultSize);

	public override string Name => Expected.Name;
	public override int Size => Expected.Size;
	public int Scale => Mathf.CeilToInt(Size * Size / Size);
	public bool IsComplete => Tiles
		.All(pair => Expected.States.IsCorrect(position: pair.Key, current: pair.Value));

	public IReadOnlyList<IReadOnlyList<int>> RowHints
	{
		get
		{
			if (field is not null) return field;
			List<int>[] hints = new List<int>[Size];
			for (int i = 0; i < Size; i++)
			{
				hints[i] = Tiles.CalculateHints(new(Display.Side.Row, i));
			}
			return field = hints;
		}
	}
	public IReadOnlyList<IReadOnlyList<int>> ColumnHints
	{
		get
		{
			if (field is not null) return field;
			List<int>[] hints = new List<int>[Size];
			for (int i = 0; i < Size; i++)
			{
				hints[i] = Tiles.CalculateHints(new(Display.Side.Column, i));
			}
			return field = hints;
		}
	}
	public SaveData() { }
	public SaveData(PuzzleData expected) => Expected = expected;

	public ImageTexture AsIcon(IColours colours, int pixelSize = 16)
	{
		Image image = Tiles.AsIcon(GetColor, Size, pixelSize);
		image.Rotate90(ClockDirection.Clockwise);
		return ImageTexture.CreateFromImage(image: image);

		Color GetColor(Vector2I position, Mode mode)
		{
			const int chunkSize = Tile.Pool.ChunkSize;
			bool alternative = (position.X / chunkSize + position.Y / chunkSize) % 2 == 0;
			return colours.NonogramTileBackground(mode, alternative);
		}
	}

	public SaveData CloneCurrentToExpected() => this with
	{
		Expected = Expected with { Tiles = Tiles }
	};
	public SaveData Clear()
	{
		foreach (Vector2I key in Tiles.Keys)
		{
			Tiles[key] = Mode.Clear;
		}
		return this;
	}
	public IEnumerable<Vector2I> InLine(Vector2I position, Display.Side side, Mode without = Mode.Filled)
	{
		if (!IsLineComplete(position, side)) { yield break; }
		foreach ((Vector2I linePosition, Mode lineMode) in Tiles.InLine(position, side))
		{
			if (lineMode == without) continue;
			yield return linePosition;
		}
	}
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

	internal void BlockCompletedLines(Tile.Pool tiles, Vector2I position)
	{
		BlockCompletedLine(Display.Side.Row);
		BlockCompletedLine(Display.Side.Column);

		void BlockCompletedLine(Display.Side side)
		{
			foreach (var linePosition in InLine(position, side))
			{
				Tile tile = tiles.GetOrCreate(linePosition);
				if (tile.Mode is Mode.Blocked) continue;
				tiles.GetOrCreate(linePosition).Mode = Mode.Blocked;
				ChangeState(position: linePosition, mode: Mode.Blocked);
				_ = tiles.TryLock(linePosition);
			}
		}
	}
}
