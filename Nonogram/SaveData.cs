using System.Text.Json.Serialization;
using Godot;

namespace RSG.Nonogram;

using Mode = Display.TileMode;
public interface IHaveExpected
{
	PuzzleData Expected { get; }

	bool IsCorrectlyFilled(Vector2I position);
	bool IsCorrectlyBlocked(Vector2I position);
	bool MatchesExpected(Vector2I position);
}
public sealed partial record SaveData : Display.Data, IHaveExpected
{
	public interface IEvents
	{
		void Changed(Vector2I position);
		void Completed();
	}
	public static void HandleInput<TCurrent>(TCurrent current, Vector2I position, Mode input)
	where TCurrent :
		ITiles<TCurrent>,
		Settings.IHave,
		Tile.ILocker,
		IDisplayType,
		IAlternate,
		IHave
	{
		input.PlayAudio();
		current.Puzzle.ChangeState(position, mode: input);
		if (current.Settings.LineCompleteBlockRest)
		{
			current.Puzzle.BlockCompletedLine(current, side: Display.Side.Row, position);
			current.Puzzle.BlockCompletedLine(current, side: Display.Side.Column, position);
		}
	}

	public interface IHave { SaveData Puzzle { get; } }
	public Action? Changed { get; set; }
	public IEvents? Events { get; set; }
	public PuzzleData Expected
	{
		get; init
		{
			field = value;
			Name = value.Name;
			Tiles.ForceMatch(value.States, () => Mode.Clear);
		}
	} = new(size: DefaultSize);
	public TimeSpan TimeTaken { get; set { field = value; Changed?.Invoke(); } } = TimeSpan.Zero;
	[JsonConverter(typeof(Vector2IDictionaryConverter<Mode>))]
	public override Dictionary<Vector2I, Mode> Tiles
	{
		protected get; init
		{
			value.ForceMatch(Expected.States, () => Mode.Clear);
			field = value;
		}
	} = CreateTiles(size: DefaultSize);

	public override string Name => Expected.Name;
	public override int Size => Expected.Size;
	public int Scale => Mathf.CeilToInt(Size * Size / Size);
	public bool IsComplete => Tiles.Keys.All(MatchesExpected);

	private IEvents? _events;

	public SaveData() { }

	public bool IsLineComplete(Vector2I position, Display.Side side) => Tiles
		.InLine(position, side)
		.Select(pair => pair.Key)
		.All(MatchesExpected);

	public bool IsCorrectlyBlocked(Vector2I position) => MatchesExactly(position, target: Mode.Blocked);
	public bool IsCorrectlyFilled(Vector2I position) => MatchesExactly(position, target: Mode.Filled);
	public bool MatchesExpected(Vector2I position) => MatchesNormalized(position);

	public SaveData ConnectTo(IEvents value)
	{
		_events = value;
		return this;
	}
	public SaveData Disconnect(IEvents value)
	{
		_events = value;
		return this;
	}

	private void BlockCompletedLine<T>(T config, Display.Side side, Vector2I position)
	where T : ITiles<T>, Tile.ILocker, IDisplayType
	{
		if (!IsLineComplete(position, side)) { return; }
		foreach ((Vector2I linePosition, Mode lineMode) in Tiles.InLine(position, side))
		{
			if (lineMode is Mode.Filled) continue;
			ChangeState(position: linePosition, mode: Mode.Blocked);
			_ = config.TryLock(position);
		}
	}
	private void ChangeState(Vector2I position, Mode mode)
	{
		Assert(Tiles.ContainsKey(position), "given position is not already in the base dictionary");
		Tiles[position] = mode;
		_events?.Changed(position);
		if (IsComplete) _events?.Completed();
	}
	private bool MatchesExactly(Vector2I position, Mode? target = null)
	{
		Assert(Tiles.ContainsKey(position), "given position is not already in current state");
		Assert(Expected.States.ContainsKey(position), "given position is not already in the expected state");

		Mode current = Tiles[position];
		Mode expected = Expected.States[position];
		target ??= expected;
		return target == current && current == expected;
	}
	private bool MatchesNormalized(Vector2I position, Mode? target = null, bool requireBlocked = false)
	{
		Assert(Tiles.ContainsKey(position), "given position is not already in current state");
		Assert(Expected.States.ContainsKey(position), "given position is not already in the expected state");

		Mode current = Tiles[position];
		Mode expected = Expected.States[position];
		target ??= expected;
		if (requireBlocked) return target == current && current == expected;
		current = current.Normalize();
		target = target?.Normalize();
		return target == current && current == expected;
	}

}
