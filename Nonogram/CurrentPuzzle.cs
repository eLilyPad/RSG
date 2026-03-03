using Godot;

namespace RSG.Nonogram;

using static Display;
using Puzzles = PuzzleManager;

public interface IHavePuzzleSettings { Settings Settings { get; } }

public sealed record class CurrentPuzzle
{
	private const TileMode defaultValue = TileMode.Clear;

	public PuzzleTimer Timer { get; }
	public Puzzles.IHaveEvents? EventHandler { get; set; }
	public Type Type { get; set => this.ChangeType(previous: field, current: field = value); } = Type.Studio;
	public Settings Settings
	{
		get; set
		{
			field = value;
			EventHandler?.SettingsChanged();
		}
	} = new Settings();
	public SaveData Puzzle
	{
		private get; set
		{
			if (value is null) return;

			NonogramStudioBar.PuzzleTabContainer puzzleTab = UI.Studio.PuzzleTab;
			_listener.Replace(field, value);
			field = value;
			Puzzles.Instance.Puzzles[field.Name] = field;
			Timer.Elapsed = field.TimeTaken;
			puzzleTab.EditableName.Text = field.Name;
			puzzleTab.PuzzleSize.Value = field.Size;
			UI.PuzzleSize = UI.Display.TilesGrid.Columns = field.Size;
		}
	} = new();
	public bool PuzzleReady => Puzzle.Expected.States.Any(p => p.Value is not defaultValue);
	public string CompletionDialogueName => Puzzle.Expected.DialogueName;

	public NonogramContainer UI { get; }

	private IImmutableDictionary<Vector2I, TileMode> CurrentStates => Type.InputData(Puzzle).States;

	private readonly GameTimer _timer;
	private readonly PuzzleHints _hints;
	private readonly PuzzleTiles _tiles;
	private readonly PuzzleModifier _puzzleModifier;
	private readonly PuzzleListener _listener;
	internal CurrentPuzzle()
	{
		_timer = new(Current: this);
		_tiles = new(Current: this);
		_hints = new(Current: this);
		_listener = new(Current: this);
		UI = new NonogramContainer(_tiles.Tiles, _hints.Hints) { Name = "Nonogram", Visible = false }
			.Preset(Control.LayoutPreset.FullRect)
			.SizeFlags(horizontal: Control.SizeFlags.ExpandFill, vertical: Control.SizeFlags.ExpandFill);
		Timer = new() { Provider = _timer };
		UI.Studio.PuzzleTab.Signals = _puzzleModifier = new PuzzleModifier(this);
	}
	public void ClearPuzzle()
	{
		Puzzle.Clear();
		UI.PuzzleSize = Puzzle.Size;
	}

	private CurrentPuzzle? TryGetValidInput(Vector2I position, out TileMode input)
	{
		Assert(CurrentStates.ContainsKey(position), $"No current tile in the data");
		input = PressedMode;
		return CurrentStates[position].IsValidInput(ref input) ? this : null;
	}
	private CurrentPuzzle ChangeTileMode(Vector2I position, Tile tile, TileMode mode)
	{
		Type.InputData(Puzzle).ChangeState(position, mode);
		tile.Mode = mode;
		mode.PlayAudio();
		return this;
	}
	private CurrentPuzzle BlockCompletedLines(Vector2I position)
	{
		if (Type is Type.Game && Settings.LineCompleteBlockRest)
		{
			Puzzle.BlockCompletedLines(_tiles.Tiles, position);
		}
		return this;
	}
	private CurrentPuzzle TryStartTimer(TileMode input)
	{
		if (_timer.ShouldStartTimer(mode: input)) Timer.TryStart();
		return this;
	}
	private sealed class PuzzleListener(CurrentPuzzle Current)
	{
		public void Replace(SaveData previous, SaveData next)
		{
			previous.Modified -= SaveTilesChanged;
			previous.Expected.Modified -= PuzzleTilesChanged;
			next.Modified += SaveTilesChanged;
			next.Expected.Modified += PuzzleTilesChanged;
		}
		public void PuzzleTilesChanged(Vector2I _)
		{
			Current._hints.Hints.Refresh();
		}
		public void SaveTilesChanged(Vector2I position)
		{
			if (Current.Type is not Type.Game) return;
			Current._tiles.Tiles.TryLock(position);
			if (!Current.Puzzle.IsComplete || Current.EventHandler is null) return;
			Current.EventHandler.Completed(Current.Puzzle);
		}
	}
	private sealed class PuzzleModifier(CurrentPuzzle Current) : IChangePuzzle
	{
		public void ModifyName(string value) => Current.Puzzle.ModifyName(value).Save();
		public void ModifySize(double value) => Current.Puzzle = Current.Puzzle
			.Clone(size: double.ConvertToInteger<int>(value))
			.Save();
	}
	private sealed class PuzzleHints(CurrentPuzzle Current) : Hints.IProvider
	{
		public Hints Hints => field ??= new(this);
		public Node Parent(HintPosition position) => Current.UI.Display.HintsParent(side: position.Side);
		public string TextLineAt(HintPosition position) => Current.Puzzle.Expected.Hints.TextLineAt(position);
	}
	private sealed class PuzzleTiles(CurrentPuzzle Current) : Tile.IProvider
	{
		public Tile.Pool Tiles => field ??= new(this)
		{
			LockRules = new() { Rules = [ShouldLockFilledTiles, ShouldLockBlockedTiles] }
		};
		public Node Parent() => Current.UI.Display.TilesGrid;
		public TileMode State(Vector2I position) => Current.CurrentStates.GetValueOrDefault(position, defaultValue);
		public void OnActivate(Vector2I position, Tile tile)
		{
			if (tile.Locked) return;
			Current
				.TryGetValidInput(position, out TileMode mode)
				?.ChangeTileMode(position, tile, mode)
				.BlockCompletedLines(position)
				.TryStartTimer(mode)
				.Puzzle.Save();
		}

		private bool ShouldLockFilledTiles(Vector2I position) => Current.Type is Type.Game
			&& Current.Settings.LockCompletedFilledTiles
			&& Current.Puzzle.IsCorrectlyFilled(position);
		private bool ShouldLockBlockedTiles(Vector2I position) => Current.Type is Type.Game
			&& Current.Settings.LockCompletedBlockedTiles
			&& Current.Puzzle.IsCorrectlyBlocked(position);
	}
	private sealed class GameTimer(CurrentPuzzle Current) : PuzzleTimer.IProvider
	{
		public Settings Settings => Current.Settings;
		public bool ShouldStartTimer(TileMode mode) => Current.Type is Type.Game && mode is TileMode.Filled;
		public void TimeChanged(string value)
		{
			Current.Puzzle.TimeTaken = Current.Timer?.Elapsed ?? TimeSpan.Zero;
			Current.UI.Display.Timer.Time.Text = "[font_size=30]" + value;
		}
	}

}