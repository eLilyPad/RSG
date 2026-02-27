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
public interface IPuzzleHints
{
	string TextLineAt(HintPosition position);
	void Recalculate(HintPosition position);
	void Recalculate(Vector2I position);
}

public sealed partial class SaveData : Data, IPuzzleState
{

	public PuzzleData Expected
	{
		get; init
		{
			field = value;
			Expected.Modified += Hints.Recalculate;
		}
	} = new();
	public TimeSpan TimeTaken { get; set; } = TimeSpan.Zero;
	[JsonConverter(typeof(Vector2IDictionaryConverter<Mode>))]
	public override Dictionary<Vector2I, Mode> Tiles { protected get; init; } = CreateTiles(DefaultSize);

	public override string Name => Expected.Name;
	public override int Size => Expected.Size;
	public int Scale => Mathf.CeilToInt(Size * Size / Size);
	public bool IsComplete => Tiles
		.All(pair => Expected.States.IsCorrect(position: pair.Key, current: pair.Value));

	public IPuzzleHints Hints => _hints;

	private readonly ExpectedHints _hints;
	public SaveData() => _hints = new(this);
	public SaveData(PuzzleData expected) => (_hints, Expected) = (new(this), expected);
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
	public SaveData Clear()
	{
		foreach (Vector2I key in Tiles.Keys) Tiles[key] = Mode.Clear;
		return this;
	}
	//public IEnumerable<(Vector2I pos, Mode current)> CorrectInLine(Vector2I position)
	//{
	//	foreach ((Vector2I pos, Mode current) in InLine(position))
	//	{
	//		if (!Expected.States.IsCorrect(position: pos, current: current)) return false;
	//	}
	//}
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
		Assert(Expected.States.ContainsKey(position), $"No expected tile in the data");
		Assert(States.ContainsKey(position), $"No current tile in the data");
		return States[position] is Mode.Blocked
			&& Expected.States[position] is Mode.Clear;
	}
	public bool IsCorrectlyFilled(Vector2I position)
	{
		Assert(Expected.States.ContainsKey(position), $"No expected tile in the data");
		Assert(States.ContainsKey(position), $"No current tile in the data");
		return Mode.Filled.AllEqual(Expected.States[position], States[position]);
	}
	internal void BlockCompletedLines(Tile.Pool tiles, Vector2I position)
	{
		foreach ((Vector2I pos, Mode current) in InLines(position))
		{
			if (current is not Mode.Clear) continue;
			Tile tile = tiles.GetOrCreate(pos);
			if (tile.Mode is Mode.Blocked) continue;
			tile.Mode = Mode.Blocked;
			ChangeState(pos, mode: Mode.Blocked);
			_ = tiles.TryLock(pos);
		}

	}
}
