using Godot;

namespace RSG.Nonogram;

using static Display;
using Puzzles = PuzzleManager;

public interface IHavePuzzleSettings { Settings Settings { get; } }

public interface IHandleDisplaysPressed
{
	void GamePuzzleDisplayPressed(UI.MainMenu menu, SaveData data);
	void StudioPuzzleDisplayPressed(SaveData data);
}

public sealed record class CurrentPuzzle : IHandleDisplaysPressed
{
	private const TileMode defaultValue = TileMode.Clear;

	public PuzzleTimer Timer { get; }
	public Puzzles.IHaveEvents? EventHandler { get; set; }
	public NonogramContainer UI { get; }
	public Action<SaveData> PuzzleCompleted { private get => Puzzle.Completed; set => Puzzle.Completed = value; }
	public Type Type { get; set => Set(ref field, value); } = Type.Studio;
	public Settings Settings { get; set => Set(ref field, value); } = new();
	public SaveData Puzzle { private get; set => Set(ref field, value.Save()); } = new();
	public bool PuzzleReady => Puzzle.Expected.States.Any(p => p.Value is not defaultValue);
	public string Name => Puzzle.Name;
	public string CompletionDialogueName => Puzzle.Expected.DialogueName;

	private readonly GameTimer _timerHandler;
	private readonly PuzzleHints _hints;
	private readonly PuzzleTiles _tiles;
	public ISaveListener? SaveListener { get; set; }
	internal CurrentPuzzle()
	{
		_timerHandler = new(Current: this);
		_tiles = new(Current: this);
		_hints = new(Current: this);
		UI = new NonogramContainer(_tiles.Tiles, _hints.Hints)
		{
			Name = "Nonogram",
			Visible = false
		}
		.Preset(Control.LayoutPreset.FullRect)
		.SizeFlags(both: Control.SizeFlags.ExpandFill);
		Timer = new() { Provider = _timerHandler };
		UI.Studio.PuzzleTab.Signals = new PuzzleModifier(this);
	}
	public void ClearPuzzle()
	{
		Puzzle.Clear();
		UI.Refresh();
	}
	public void TryLockTile(Vector2I position)
	{
		Tile.Pool tiles = UI.Tiles;
		if (Type is not Type.Game) return;
		tiles.TryLock(position);
	}
	public void GamePuzzleDisplayPressed(UI.MainMenu menu, SaveData data)
	{
		if (!GodotObject.IsInstanceValid(menu.Levels)) return;
		if (!GodotObject.IsInstanceValid(menu)) return;
		Type = Type.Game;
		Puzzle = data;
		UI.Show();
		menu.Levels.Hide();
		menu.Hide();
	}
	public void StudioPuzzleDisplayPressed(SaveData data)
	{
		Puzzle = data;
		Type = Type.Studio;
		UI.Show();
	}
	public ImageTexture StudioIcon(IColours colours) => Puzzle.Expected.AsIcon(colours);
	public void RefreshCurrentStudioIcon(IColours colours, PuzzleSelector.PuzzleDisplay display)
	{
		display.Button.Icon = Puzzle.Expected.AsIcon(colours);
	}
	private void Set(ref SaveData field, SaveData value)
	{
		SaveListener?.Replace(field, value);
		value.Completed = field.Completed;
		(Timer.Elapsed, UI.PuzzleSize, UI.Studio.PuzzleTab.EditableName.Text) = field = value;
	}
	private void Set(ref Settings field, Settings value)
	{
		field = value;
		EventHandler?.SettingsChanged();
	}
	private void Set(ref Type field, Type value)
	{
		if (field == value) return;
		NonogramStudioBar studio = UI.Studio;
		TimerContainer timer = UI.Display.Timer;
		Tile.Pool tiles = UI.Tiles;
		UI.Display.Name = value.AsName();
		var previous = field;
		field = value;
		switch (value)
		{
			case Type.Game:
				timer.Show();
				studio.Hide();
				if (previous is Type.Studio)
				{
					Puzzle.Clear();
					UI.Refresh();
				}
				break;
			case Type.Studio:
				timer.Hide();
				studio.Show();
				tiles.Refresh();
				tiles.UnLockAll();
				break;
		}
	}
	private CurrentPuzzle ClearWhenInputMatchesCurrent(
		Vector2I position,
		ref TileMode mode,
		out TileMode current
	)
	{
		current = Type switch
		{
			Type.Studio => Puzzle.Expected.States[position],
			_ => Puzzle.States[position]
		};

		mode = mode.ToClearWhenSame(current);
		return this;
	}
	private CurrentPuzzle ChangeTileMode(Vector2I position, Tile tile, TileMode mode)
	{
		Data data = Type switch
		{
			Type.Studio => Puzzle.Expected,
			_ => Puzzle
		};
		data.ChangeState(position, mode);
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
		if (_timerHandler.ShouldStartTimer(mode: input)) Timer.TryStart();
		return this;
	}
	private sealed class PuzzleListener(CurrentPuzzle Current) : ISaveListener
	{
		public void PuzzleTilesChanged(Vector2I _) => Current._hints.Hints.Refresh();
		public void SaveTilesChanged(Vector2I position)
		{
			if (Current.Type is not Type.Game) return;
			Current._tiles.Tiles.TryLock(position);
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
		public TileMode State(Vector2I position)
		{
			Data data = Current.Type switch
			{
				Type.Studio => Current.Puzzle.Expected,
				_ => Current.Puzzle
			};
			TileMode tileMode = data.States.GetValueOrDefault(position, defaultValue);
			return tileMode;
		}

		public void OnActivate(Vector2I position, Tile tile)
		{
			const TileMode ignored = TileMode.Clear;
			if (tile.Locked) return;
			if (!TryGetMouseInput(ignoredValue: ignored, mode: out TileMode mode)) return;
			Current.ClearWhenInputMatchesCurrent(position, ref mode, current: out TileMode current);
			if (mode.AllEqual(ignored, current)) return;
			Current
				.ChangeTileMode(position, tile, mode)
				.BlockCompletedLines(position)
				.TryStartTimer(mode)
				.Puzzle.Save();
		}
		private static bool TryGetMouseInput(in TileMode ignoredValue, out TileMode mode)
		{
			bool isFilledPressed = Input.IsMouseButtonPressed(FillButton);
			bool isBlockPressed = Input.IsMouseButtonPressed(BlockButton);
			if (isFilledPressed)
			{
				mode = TileMode.Filled;
				return true;
			}
			if (isBlockPressed)
			{
				mode = TileMode.Blocked;
				return true;
			}
			mode = ignoredValue;
			return false;
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