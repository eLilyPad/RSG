using Godot;

namespace RSG.Nonogram;

using static Display;

public interface IHavePuzzleSettings { Settings Settings { get; } }

public sealed partial class PuzzleManager
{
	public sealed record class CurrentPuzzle : Hints.IProvider, Tile.IProvider, PuzzleTimer.IProvider
	{
		public PuzzleTimer Timer { get; }
		public IHaveEvents? EventHandler { get; set; }
		public bool PuzzleReady { get; private set; } = false;
		public Type Type { get; set => UI.ChangeType(ref field, value); } = Type.Display;
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

				Instance.Puzzles[field.Name] = field;
				Timer.Elapsed = field.TimeTaken;
				PuzzleReady = true;
				UI.PuzzleSize = UI.Display.TilesGrid.Columns = field.Size;
			}
		}
		public string CompletionDialogueName => Puzzle.Expected.DialogueName;

		public NonogramContainer UI;

		internal CurrentPuzzle()
		{
			List<Func<Vector2I, bool>> rules = [
				(position) => Settings.LockCompletedFilledTiles && Puzzle.IsCorrectlyFilled(position),
				(position) => Settings.LockCompletedBlockedTiles && Puzzle.IsCorrectlyBlocked(position),
			];
			UI = new NonogramContainer(rules, puzzle: this) { Name = "Nonogram", Visible = false }
				.Preset(Control.LayoutPreset.FullRect)
				.SizeFlags(horizontal: Control.SizeFlags.ExpandFill, vertical: Control.SizeFlags.ExpandFill);
			Timer = new() { Provider = this };
			Puzzle = new() { };
			PuzzleReady = false;
		}
		void PuzzleTimer.IProvider.TimeChanged(string value)
		{
			Puzzle.TimeTaken = Timer?.Elapsed ?? TimeSpan.Zero;
			UI.Display.Timer.Time.Text = "[font_size=30]" + value;
		}
		Node Hints.IProvider.Parent(HintPosition position) => UI.Display.HintsParent(side: position.Side);
		string Hints.IProvider.Text(HintPosition position) => Puzzle.Expected.States.CalculateHints(position);
		Node Tile.IProvider.Parent() => UI.Display.TilesGrid;
		TileMode Tile.IProvider.State(Vector2I position)
		{
			return Puzzle.States.GetValueOrDefault(position, TileMode.Clear);
		}
		void Tile.IProvider.OnActivate(Vector2I position, Tile tile)
		{
			SaveData.InputEvent input = new(position, Settings, Type, PressedMode);
			Puzzle.HandleUserInput(input, UI.Tiles, Timer, EventHandler);
			Save(Puzzle);
		}
	}
}
