using System.Text.Json.Serialization;
using Godot;

namespace RSG.Nonogram;

using Mode = Display.TileMode;

public sealed partial record SaveData : Display.Data
{
	internal readonly record struct InputEvent(
		Vector2I Position,
		Settings Settings,
		Display.Type Type,
		Mode Mode
	);
	internal void HandleUserInput(
		InputEvent input,
		Tile.Pool tiles,
		PuzzleTimer timer,
		PuzzleManager.IHaveEvents? eventHandler
	)
	{
		const Mode defaultValue = Mode.NULL;
		(Vector2I position, Settings settings, Display.Type _, Mode mode) = input;

		if (mode is defaultValue) return;
		Tile tile = tiles.GetOrCreate(position);

		Assert(States.ContainsKey(position), $"No current tile in the data");
		Mode current = States[position];
		Assert(tile.Mode == current, "tiles displayed mode is unsynchronized from data");

		mode = mode == current ? Mode.Clear : mode;

		if (Mode.Clear.AllEqual(current, mode)) return;
		if (tile.Locked) return;
		mode.PlayAudio();
		ChangeState(position, mode: tile.Mode = mode);

		if (settings.LineCompleteBlockRest)
		{
			BlockCompletedLine(side: Display.Side.Row);
			BlockCompletedLine(side: Display.Side.Column);
		}

		if (tiles.LockRules.ShouldLock(position)) tile.Locked = true;
		if (!timer.Running && mode is Mode.Filled) timer.Running = true;
		if (IsComplete) eventHandler?.Completed(this);

		void BlockCompletedLine(Display.Side side)
		{
			if (!IsLineComplete(position, side)) { return; }
			foreach ((Vector2I linePosition, Mode lineMode) in Tiles.InLine(position, side))
			{
				if (lineMode is Mode.Filled) continue;
				Tile tile = tiles.GetOrCreate(linePosition);
				if (tile.Mode is Mode.Blocked) continue;
				ChangeState(position: linePosition, mode: tile.Mode = Mode.Blocked);
				tile.Locked = tiles.LockRules.ShouldLock(position);
			}
		}
	}

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

	private void ChangeState(Vector2I position, Mode mode)
	{
		Assert(Tiles.ContainsKey(position), "given position is not already in the base dictionary");
		Tiles[position] = mode;
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
