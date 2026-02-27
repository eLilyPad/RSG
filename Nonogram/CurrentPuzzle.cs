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
		public string Text(HintPosition position) => Current.Puzzle.Hints.TextLineAt(position);
		//Tiles
		public Node Parent() => Current.UI.Display.TilesGrid;
		public TileMode State(Vector2I position) => Current.CurrentStates.GetValueOrDefault(position, defaultValue);
		public void OnActivate(Vector2I position, Tile tile)
		{
			Assert(Current.CurrentStates.ContainsKey(position), $"No current tile in the data");

			TileMode current = Current.CurrentStates[position];
			NonogramContainer ui = Current.UI;
			SaveData puzzle = Current.Puzzle;
			Type type = Current.Type;
			NonogramStudioBar studio = ui.Studio;
			Tile.Pool tiles = ui.Tiles;
			Hints hints = ui.Hints;
			Data data = type.InputData(puzzle);
			IImmutableDictionary<Vector2I, TileMode> state = puzzle.Expected.States;
			TileMode mode = PressedMode;

			if (!current.IsValidInput(ref mode) || tile.Locked) return;

			data.ChangeState(position, mode);
			tile.Mode = mode;
			mode.PlayAudio();

			switch (type)
			{
				case Type.Game:
					_ = tiles.TryLock(position);
					if (Settings.LineCompleteBlockRest) puzzle.BlockCompletedLines(tiles, position);
					if (puzzle.IsComplete) Current.EventHandler?.Completed(puzzle);
					Current.Timer.TryStart(tile: mode);
					break;
				case Type.Studio:
					hints.Refresh();
					//bool solvable = state.IsSolvable(puzzle.Size);
					//studio.PuzzleTab.Message.Text = $"Solvable: {solvable}";
					break;
			}

			Puzzles.Save(puzzle);
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
			field = value;

			Puzzles.Instance.Puzzles[field.Name] = field;
			Timer.Elapsed = field.TimeTaken;
			UI.Studio.PuzzleTab.EditableName.Text = field.Name;
			UI.Studio.PuzzleTab.PuzzleSize.Value = field.Size;
			UI.PuzzleSize = UI.Display.TilesGrid.Columns = field.Size;
		}
	} = new();

	public bool PuzzleReady => Puzzle.Expected.States.Any(p => p.Value is not defaultValue);
	public string CompletionDialogueName => Puzzle.Expected.DialogueName;

	public NonogramContainer UI { get; }

	private IImmutableDictionary<Vector2I, TileMode> CurrentStates => Type.InputData(Puzzle).States;

	internal CurrentPuzzle()
	{
		List<Func<Vector2I, bool>> rules = [
			(position) => Type is Type.Game
					&& Settings.LockCompletedFilledTiles
					&& Puzzle.IsCorrectlyFilled(position),
				(position) => Type is Type.Game
					&& Settings.LockCompletedBlockedTiles
					&& Puzzle.IsCorrectlyBlocked(position),
			];
		UIProviders Provider = new(Current: this);
		Tile.Pool tiles = new(Provider) { LockRules = new() { Rules = rules } };
		Hints hints = new(Provider);
		UI = new NonogramContainer(tiles, hints) { Name = "Nonogram", Visible = false }
			.Preset(Control.LayoutPreset.FullRect)
			.SizeFlags(horizontal: Control.SizeFlags.ExpandFill, vertical: Control.SizeFlags.ExpandFill);
		Timer = new() { Provider = Provider };
		UI.Studio.PuzzleTab.Signals = new PuzzleModifier(this);
	}
	public void ClearPuzzle()
	{
		Puzzle.Clear();
		UI.PuzzleSize = Puzzle.Size;
	}
}