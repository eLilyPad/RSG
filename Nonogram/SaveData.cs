using System.Text.Json.Serialization;
using Godot;
using RSG.UI;

namespace RSG.Nonogram;

using Mode = Display.TileMode;
using static Display;
public interface IPuzzleState
{
	bool IsLineComplete(Vector2I position, Side side);
	bool IsCorrectlyBlocked(Vector2I position);
	bool IsCorrectlyFilled(Vector2I position);
}

public sealed partial class SaveData : Data, IPuzzleState
{
	public PuzzleData Expected { get; init; } = new();
	public Action<SaveData> Completed { get; set; } = _ => { };
	public TimeSpan TimeTaken { get; set; } = TimeSpan.Zero;
	[JsonConverter(typeof(Vector2IDictionaryConverter<Mode>))]
	public override Dictionary<Vector2I, Mode> Tiles { protected get; init; } = CreateTiles(DefaultSize);

	public override string Name => Expected.Name;
	public override int Size => Expected.Size;
	public int Scale => Mathf.CeilToInt(Size * Size / Size);
	public bool IsComplete { get; private set; }
	//Tiles.All(pair => Expected.States.IsCorrect(position: pair.Key, current: pair.Value));

	public SaveData() { }
	public SaveData(PuzzleData expected) => Expected = expected;
	public SaveData Clone(int size)
	{
		Dictionary<Vector2I, Mode> newCurrent = CreateTiles(size);
		Dictionary<Vector2I, Mode> newExpected = CreateTiles(size);
		foreach (Vector2I key in newCurrent.Keys)
		{
			if (!Tiles.TryGetValue(key, out Mode mode)) continue;
			newCurrent[key] = mode;
		}
		foreach (Vector2I key in newExpected.Keys)
		{
			if (!Expected.States.TryGetValue(key, out Mode mode)) continue;
			newExpected[key] = mode;
		}
		SaveData value = new()
		{
			Tiles = newCurrent,
			Expected = new(size)
			{
				Name = Name,
				DialogueName = Expected.DialogueName,
				Tiles = newExpected
			}
		};
		return value;
	}
	public SaveData ModifyName(string value)
	{
		Expected.Name = value;
		return this;
	}
	public SaveData Clear()
	{
		foreach (Vector2I key in Tiles.Keys) Tiles[key] = Mode.Clear;
		return this;
	}
	public SaveData Save()
	{
		PuzzleManager.Save(this);
		return this;
	}
	public bool IsLineComplete(Vector2I position, Side side)
	{
		foreach ((Vector2I pos, Mode current) in InLine(position, side))
		{
			if (!Expected.States.IsCorrect(position: pos, current: current)) return false;
		}
		return true;
	}
	public bool IsCorrectlyBlocked(Vector2I position)
	{
		AssertHasPosition(position);
		return States[position] is Mode.Blocked
			&& Expected.States[position] is Mode.Clear;
	}
	public bool IsCorrectlyFilled(Vector2I position)
	{
		AssertHasPosition(position);
		return Mode.Filled.AllEqual(Expected.States[position], States[position]);
	}
	internal override void ChangeState(Vector2I position, Mode mode)
	{
		base.ChangeState(position, mode);
		IsComplete = Tiles.All(IsCorrect);
		if (IsComplete) Completed(this);

		bool IsCorrect(KeyValuePair<Vector2I, Mode> pair) => Expected.States.IsCorrect(position: pair.Key, current: pair.Value);
	}
	internal void BlockCompletedLines(Tile.Pool tiles, Vector2I position)
	{
		foreach ((Vector2I pos, Mode current) in InLines(position))
		{
			if (!IsLineComplete(pos, Side.Row) && !IsLineComplete(pos, Side.Column)) continue;
			if (current is not Mode.Clear) continue;
			Tile tile = tiles.GetOrCreate(pos);
			if (tile.Mode is Mode.Blocked) continue;
			tile.Mode = Mode.Blocked;
			ChangeState(pos, mode: Mode.Blocked);
		}
	}
	private void AssertHasPosition(Vector2I position)
	{
		Assert(Expected.States.ContainsKey(position), $"No expected tile in the data");
		Assert(States.ContainsKey(position), $"No current tile in the data");
	}
}
