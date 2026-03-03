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

			Hints hints = _hints.Hints;
			Tile.Pool tiles = _tiles.Tiles;
			NonogramStudioBar.PuzzleTabContainer puzzleTab = UI.Studio.PuzzleTab;

			field = value;
			Puzzles.Instance.Puzzles[field.Name] = field;
			Timer.Elapsed = field.TimeTaken;
			puzzleTab.EditableName.Text = field.Name;
			puzzleTab.PuzzleSize.Value = field.Size;
			UI.PuzzleSize = UI.Display.TilesGrid.Columns = field.Size;
			field.Modified += TilesChanged;
			field.Expected.Modified += _ => hints.Refresh();

			void TilesChanged(Vector2I position)
			{
				if (Type is not Type.Game) return;
				tiles.TryLock(position);
				if (!field.IsComplete || EventHandler is null) return;
				EventHandler.Completed(field);
			}
		}
	} = new();
	public bool PuzzleReady => Puzzle.Expected.States.Any(p => p.Value is not defaultValue);
	public string CompletionDialogueName => Puzzle.Expected.DialogueName;

	public NonogramContainer UI { get; }

	private IImmutableDictionary<Vector2I, TileMode> CurrentStates => Type.InputData(Puzzle).States;

	private readonly GameTimer _timer;
	private readonly PuzzleHints _hints;
	private readonly PuzzleTiles _tiles;
	internal CurrentPuzzle()
	{
		_timer = new(Current: this);
		_tiles = new(Current: this);
		_hints = new(Current: this);
		UI = new NonogramContainer(_tiles.Tiles, _hints.Hints) { Name = "Nonogram", Visible = false }
			.Preset(Control.LayoutPreset.FullRect)
			.SizeFlags(horizontal: Control.SizeFlags.ExpandFill, vertical: Control.SizeFlags.ExpandFill);
		Timer = new() { Provider = _timer };
		UI.Studio.PuzzleTab.Signals = new PuzzleModifier(this);
	}
	public void ClearPuzzle()
	{
		Puzzle.Clear();
		UI.PuzzleSize = Puzzle.Size;
	}

	private bool TryGetValidInput(Vector2I position, out TileMode input)
	{
		Assert(CurrentStates.ContainsKey(position), $"No current tile in the data");
		input = PressedMode;
		return CurrentStates[position].IsValidInput(ref input);
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
			if (!Current.TryGetValidInput(position, out TileMode mode) || tile.Locked) return;
			SaveData puzzle = Current.Puzzle;
			Current.Type.InputData(puzzle).ChangeState(position, mode);
			tile.Mode = mode;
			mode.PlayAudio();
			if (Current._timer.ShouldStartTimer(mode)) Current.Timer.TryStart();
			if (Current.Type is Type.Game && Current.Settings.LineCompleteBlockRest)
			{
				puzzle.BlockCompletedLines(Current.UI.Tiles, position);
			}
			puzzle.Save();
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