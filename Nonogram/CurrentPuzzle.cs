using Godot;

namespace RSG.Nonogram;

using static Display;
using Puzzles = PuzzleManager;

public interface IHavePuzzleSettings { Settings Settings { get; } }
public interface IIconize { ImageTexture ToIcon(IColours colours); }


public sealed record class CurrentPuzzle : PuzzleSelector.Display.IPressed, IIconize
{
	public static CurrentPuzzle Create(Node parent)
	{
		var value = new CurrentPuzzle();
		var ui = value.UI;
		parent.AddChild(ui);
		return value;
	}
	private const TileMode defaultValue = TileMode.Clear;
	public PuzzleTimer Timer { get; }
	public NonogramContainer UI { get; }
	public Puzzles.IHaveEvents? EventHandler { get; set; }
	public ISaveListener? SaveListener { get; set; }
	public Action<SaveData> PuzzleCompleted { private get => Puzzle.Completed; set => Puzzle.Completed = value; }

	public Type Type
	{
		get; set
		{
			if (field == value) return;
			var previous = field;
			var studio = UI.Studio;
			var timer = UI.Display.Timer;
			UI.Display.Name = value.AsName();
			field = value;
			switch (value)
			{
				case Type.Game:
					timer.Show();
					studio.Hide();
					if (previous is Type.Studio)
					{
						Puzzle.Clear();
						_tiles.Refresh();
						_hints.Refresh();
					}
					break;
				case Type.Studio:
					timer.Hide();
					studio.Show();
					_tiles.Refresh();
					_tiles.UnLockAll();
					break;
			}
		}
	} = Type.Game;
	public Settings Settings
	{
		get; set
		{
			field = value;
			EventHandler?.SettingsChanged();
		}
	} = new();
	public SaveData Puzzle
	{
		private get; set
		{
			var studioPuzzleTab = UI.Studio.PuzzleTab;
			var previous = field;
			field = value.Save();
			SaveListener?.Replace(previous, value);
			value.Completed = previous.Completed;
			studioPuzzleTab.EditableName.Text = previous.Name;
			Timer.Elapsed = previous.TimeTaken;
			ChangePuzzleSize(previous.Size);

			_tiles.Refresh();
			_hints.Refresh();
		}
	} = new();

	public IColours Colours { set => _hints.Colours = UI.BackgroundColours = value; }

	public Tile.IPool Tiles => _tiles;
	public bool PuzzleReady => Puzzle.Expected.States.Any(p => p.Value is not defaultValue);
	public string Name => Puzzle.Name;
	public string CompletionDialogueName => Puzzle.Expected.DialogueName;

	private Data TypesData => Type switch { Type.Studio => Puzzle.Expected, _ => Puzzle };
	private bool ShouldBlockLines => Type is Type.Game && Settings.LineCompleteBlockRest;


	private readonly PuzzleModifier _modifier;
	private readonly GameTimer _timerHandler;
	private readonly Tile.Locker _locker;
	private readonly PuzzleHints _hints;
	private readonly PuzzleTiles _tiles;

	private CurrentPuzzle()
	{
		_tiles = new(this);
		_hints = new(this);
		_modifier = new(this);
		_locker = new() { Rules = [ShouldLockFilledTiles, ShouldLockBlockedTiles] };
		_timerHandler = new(Current: this);
		UI = new NonogramContainer { Name = "Nonogram", Visible = false }
			.Preset(Control.LayoutPreset.FullRect)
			.SizeFlags(both: Control.SizeFlags.ExpandFill);
		Timer = new() { Provider = _timerHandler };
		UI.Studio.PuzzleTab.Signals = _modifier;
	}

	public void RefreshHints() => _hints.Refresh();
	public ImageTexture ToIcon(IColours colours) => Type switch
	{
		Type.Studio => Puzzle.Expected.AsIcon(colours),
		Type.Game => Puzzle.AsIcon(colours),
		_ => throw new InvalidOperationException("Invalid puzzle type")
	};
	public T Pressed<T>(T display, ReadOnlySpan<Control> toHide, SaveData data)
	where T : PuzzleSelector.Display
	{
		bool leftPressed = MouseButton.Left.IsPressed(), rightPressed = MouseButton.Right.IsPressed();
		Assert(
			condition: leftPressed || rightPressed,
			"Pressed event should only be triggered by mouse button input"
		);
		Assert(
			condition: toHide.AllValidInstances(),
			"Menu and levels display must be valid"
		);
		switch (display)
		{
			case PuzzleSelector.Display.Game when leftPressed:
			case PuzzleSelector.Display.Studio when rightPressed:
				Type = Type.Game;
				foreach (var c in toHide) c.Visible = false;
				break;
			case PuzzleSelector.Display.Studio when leftPressed:
				Type = Type.Studio;
				break;
		}
		Puzzle = data;
		UI.Visible = true;
		return display;
	}
	public void ChangePuzzleSize(int size)
	{
		var tilesGrid = UI.Display.TilesGrid;
		var studioPuzzleTab = UI.Studio.PuzzleTab;
		tilesGrid.Columns = size;
		studioPuzzleTab.PuzzleSize.SetValueNoSignal(size);

		_tiles.Resize(size);
		_hints.TileSize = _tiles.TileSize;
		_hints.Resize(size);

		tilesGrid.CustomMinimumSize = Mathf.CeilToInt(size) * _tiles.TileSize;
	}

	private bool ShouldLockFilledTiles(Vector2I position) => Type is Type.Game
		&& Settings.LockCompletedFilledTiles
		&& Puzzle.IsCorrectlyFilled(position);
	private bool ShouldLockBlockedTiles(Vector2I position) => Type is Type.Game
		&& Settings.LockCompletedBlockedTiles
		&& Puzzle.IsCorrectlyBlocked(position);

	private CurrentPuzzle ToClearWhenMatches(Vector2I position, ref TileMode mode, out TileMode current)
	{
		current = TypesData.States[position];
		mode = mode.ToClearWhenSame(current);
		return this;
	}
	private CurrentPuzzle ChangeTileMode(Vector2I position, Tile tile, TileMode mode)
	{
		TypesData.ChangeState(position, mode);
		tile.Mode = mode;
		mode.PlayAudio();
		return this;
	}
	private CurrentPuzzle BlockCompletedLines(Vector2I position)
	{
		if (!ShouldBlockLines) return this;
		Puzzle.BlockCompletedLines(_tiles, position);
		return this;
	}
	private CurrentPuzzle TryStartTimer(TileMode input)
	{
		if (!_timerHandler.ShouldStartTimer(mode: input)) return this;
		Timer.TryStart();
		return this;
	}


	private sealed class PuzzleModifier(CurrentPuzzle Current) : IChangePuzzle
	{
		public void ModifyName(string value) => Current.Puzzle.ModifyName(value).Save();
		public void ModifySize(double value) => Current.Puzzle = Current.Puzzle
			.Clone(size: double.ConvertToInteger<int>(value))
			.Save();
	}
	private sealed class PuzzleHints(CurrentPuzzle Current) : NodePool<HintPosition, Hint>
	{
		public Vector2 TileSize { get; set; } = Vector2.Zero;
		public IColours Colours { private get; set; } = Core.Colours;

		public void Resize(int length)
		{
			Clear();
			IEnumerable<HintPosition> hintValues = HintPosition.AsRange(length);
			foreach (HintPosition key in hintValues)
			{
				_ = GetOrCreate(key);
			}
			Clear(exceptions: hintValues);
		}
		public void Refresh()
		{
			foreach ((HintPosition position, Hint hint) in _nodes)
			{
				hint.Label.Text = Current.Puzzle.Expected.Hints.TextLineAt(position);
			}
		}
		protected override Node Parent(HintPosition position) => Current.UI.Display.HintsParent(side: position.Side);
		protected override Hint Create(HintPosition position)
		{
			Hint hint = Hint.Create(position, Colours);
			Parent(position).AddChild(hint);
			hint.CustomMinimumSize = TileSize;
			return hint;
		}
	}
	private sealed class PuzzleTiles(CurrentPuzzle Current) : NodePool<Vector2I, Tile>, Tile.IProvider, Tile.IPool
	{
		private readonly List<Func<Vector2I, bool>> _rules = [
			Current.ShouldLockFilledTiles,
			Current.ShouldLockBlockedTiles
		];
		public const TileMode IgnoredValue = TileMode.Clear;
		public Vector2 TileSize { get; private set; } = Vector2.One;

		public void OnActivate(Vector2I position, Tile tile)
		{
			if (tile.Locked) return;
			TileMode input = FillButton.GetPressed() ?? BlockButton.GetPressed() ?? IgnoredValue;
			if (input is IgnoredValue) return;
			Current.ToClearWhenMatches(position, mode: ref input, current: out TileMode current);
			if (input.AllEqual(IgnoredValue, current)) return;
			Current
				.ChangeTileMode(position, tile, mode: input)
				.BlockCompletedLines(position)
				._timerHandler.TryStart(input)
				.Puzzle.Save();
		}
		public TileMode State(Vector2I position) => Current.TypesData.States
			.GetValueOrDefault(position, defaultValue);

		public void UnLockAll() => _nodes.Values.LockTiles(false);
		public bool ShouldLock(Vector2I position) => _rules.Any(rule => rule(position));
		public bool TryLock(Vector2I position)
		{
			Tile tile = GetOrCreate(position);
			bool locked = ShouldLock(position);
			if (locked) tile.Locked = true;
			return locked;
		}

		public void Resize(int value)
		{
			Clear();
			Vector2I size = Vector2I.One * value;
			IEnumerable<Vector2I> tileValues = size.GridRange();
			bool firstTile = true;
			foreach (Vector2I position in tileValues)
			{
				Tile tile = GetOrCreate(position);
				if (firstTile)
				{
					TileSize = tile.Size;
					firstTile = false;
				}
			}
			Clear(exceptions: tileValues);
		}
		public void Refresh()
		{
			foreach ((Vector2I position, Tile tile) in _nodes)
			{
				tile.Mode = State(position);
				tile.Locked = Current._locker.ShouldLock(position);
			}
		}

		protected override Node Parent(Vector2I position) => Current.UI.Display.TilesGrid;
		protected override Tile Create(Vector2I position)
		{
			Tile tile = Tile.Create(position);
			Parent(position).AddChild(tile);

			tile.Button.ButtonDown += () => OnActivate(position, tile);
			tile.Button.MouseExited += () => HoverAllInLines(position, false);
			tile.Button.MouseEntered += () =>
			{
				OnActivate(position, tile);
				HoverAllInLines(position, true);
			};
			return tile;
		}

		private void HoverAllInLines(Vector2I position, bool value) => _nodes
			.AllInLines(position)
			.HoverTiles(value);
	}
	private sealed class GameTimer(CurrentPuzzle Current) : PuzzleTimer.IProvider
	{
		public Settings Settings => Current.Settings;
		public CurrentPuzzle TryStart(TileMode mode)
		{
			if (!Current._timerHandler.ShouldStartTimer(mode)) return Current;
			Current.Timer.TryStart();
			return Current;
		}
		public bool ShouldStartTimer(TileMode mode) => Current.Type is Type.Game && mode is TileMode.Filled;
		public void TimeChanged(string value)
		{
			Current.Puzzle.TimeTaken = Current.Timer.Elapsed;
			Current.UI.Display.Timer.Time.Text = "[font_size=30]" + value;
		}
	}
}