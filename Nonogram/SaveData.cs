using System.Text.Json.Serialization;
using Godot;

namespace RSG.Nonogram;

using Mode = Display.TileMode;

public sealed partial record SaveData : Display.Data
{

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
				ChangeMode(position: linePosition, mode: Mode.Blocked, tiles: tiles);
			}
		}
	}
	private void ChangeMode(Vector2I position, Mode mode, Tile.Pool tiles)
	{
		tiles.GetOrCreate(position).Mode = mode;
		ChangeState(position, mode);
		_ = tiles.TryLock(position);
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
