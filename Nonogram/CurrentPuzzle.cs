using Godot;

namespace RSG.Nonogram;

using static Display;
using Puzzles = PuzzleManager;

public interface IHavePuzzleSettings { Settings Settings { get; } }

public sealed record class CurrentPuzzle
{
	private sealed class PuzzleModifier(CurrentPuzzle Current) : IChangePuzzle
	{
		public void ModifyName(string value)
		{
			Current.Puzzle.ModifyName(value);
			Puzzles.Save(Current.Puzzle);
		}
		public void ModifySize(double value)
		{
			int size = double.ConvertToInteger<int>(value);
			Current.Puzzle = Current.Puzzle.Clone(size);
			Puzzles.Save(Current.Puzzle);
		}
	}
	private sealed class UIProviders(CurrentPuzzle Current) : Hints.IProvider, Tile.IProvider, PuzzleTimer.IProvider
	{
		public Settings Settings => Current.Settings;
		//Hints
		public Node Parent(HintPosition position) => Current.UI.Display.HintsParent(side: position.Side);
		public string Text(HintPosition position) => Current.Puzzle.PuzzleHints.TextLineAt(position);
		//Tiles
		public Node Parent() => Current.UI.Display.TilesGrid;
		public TileMode State(Vector2I position) => Current.CurrentStates.GetValueOrDefault(position, defaultValue);
		public void OnActivate(Vector2I position, Tile tile)
		{
			Assert(Current.CurrentStates.ContainsKey(position), $"No current tile in the data");

			TileMode current = Current.CurrentStates[position];
			SaveData puzzle = Current.Puzzle;
			Type type = Current.Type;
			Tile.Pool tiles = Current.UI.Tiles;
			Data data = type.InputData(puzzle);
			TileMode mode = PressedMode;

			if (!current.IsValidInput(ref mode) || tile.Locked) return;

			data.ChangeState(position, mode);
			tile.Mode = mode;
			mode.PlayAudio();

			Puzzles.Save(puzzle);

			if (type is Type.Game) Current.Timer.TryStart(tile: mode);
			if (type is Type.Game && Settings.LineCompleteBlockRest) puzzle.BlockCompletedLines(tiles, position);
		}
		//Timer
		public void TimeChanged(string value)
		{
			Current.Puzzle.TimeTaken = Current.Timer?.Elapsed ?? TimeSpan.Zero;
			Current.UI.Display.Timer.Time.Text = "[font_size=30]" + value;
		}
	}

	private const TileMode defaultValue = TileMode.Clear;

	public PuzzleTimer Timer { get; }
	public Puzzles.IHaveEvents? EventHandler { get; set; }
	public Type Type { get; set => this.ChangeType(previous: field, current: field = value); } = Type.Game;
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

			Hints hints = UI.Hints;
			Tile.Pool tiles = UI.Tiles;
			NonogramStudioBar.PuzzleTabContainer puzzleTab = UI.Studio.PuzzleTab;

			field = value;
			Puzzles.Instance.Puzzles[field.Name] = field;
			Timer.Elapsed = field.TimeTaken;
			puzzleTab.EditableName.Text = field.Name;
			puzzleTab.PuzzleSize.Value = field.Size;
			UI.PuzzleSize = UI.Display.TilesGrid.Columns = field.Size;
			field.Modified += TilesChanged;

			void TilesChanged(Vector2I position)
			{
				switch (Type)
				{
					case Type.Game:
						tiles.TryLock(position);
						if (field.IsComplete) EventHandler?.Completed(field);
						break;
					case Type.Studio:
						hints.Refresh();
						//bool solvable = state.IsSolvable(puzzle.Size);
						//studio.PuzzleTab.Message.Text = $"Solvable: {solvable}";
						break;
				}
			}
		}
	} = new();
	public bool PuzzleReady => Puzzle.Expected.States.Any(p => p.Value is not defaultValue);
	public string CompletionDialogueName => Puzzle.Expected.DialogueName;

	public NonogramContainer UI { get; }

	private IImmutableDictionary<Vector2I, TileMode> CurrentStates => Type.InputData(Puzzle).States;

	internal CurrentPuzzle()
	{
		UIProviders Provider = new(Current: this);
		Tile.Locker locker = new() { Rules = [ShouldLockFilledTiles, ShouldLockBlockedTiles] };
		Tile.Pool tiles = new(Provider) { LockRules = locker };
		Hints hints = new(Provider);
		UI = new NonogramContainer(tiles, hints) { Name = "Nonogram", Visible = false }
			.Preset(Control.LayoutPreset.FullRect)
			.SizeFlags(horizontal: Control.SizeFlags.ExpandFill, vertical: Control.SizeFlags.ExpandFill);
		Timer = new() { Provider = Provider };
		UI.Studio.PuzzleTab.Signals = new PuzzleModifier(this);

		bool ShouldLockFilledTiles(Vector2I position) => Type is Type.Game
			&& Settings.LockCompletedFilledTiles
			&& Puzzle.IsCorrectlyFilled(position);
		bool ShouldLockBlockedTiles(Vector2I position) => Type is Type.Game
			&& Settings.LockCompletedFilledTiles
			&& Puzzle.IsCorrectlyFilled(position);
	}
	public void ClearPuzzle()
	{
		Puzzle.Clear();
		UI.PuzzleSize = Puzzle.Size;
	}
}