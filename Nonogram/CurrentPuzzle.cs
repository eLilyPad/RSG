using Godot;

namespace RSG.Nonogram;

using static Display;

public interface IHavePuzzleSettings { Settings Settings { get; } }

public sealed partial class PuzzleManager
{
	public sealed record class CurrentPuzzle :
		Hints.IProvider,
		Tile.IProvider,
		PuzzleTimer.IProvider,
		IChangePuzzle
	{
		public PuzzleTimer Timer { get; }
		public IHaveEvents? EventHandler { get; set; }
		public bool PuzzleReady => Puzzle.Expected.States.Any(p => p.Value != TileMode.Clear);
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

				Instance.Puzzles[field.Name] = field;
				Timer.Elapsed = field.TimeTaken;
				UI.Studio.PuzzleTab.EditableName.Text = field.Name;
				UI.Studio.PuzzleTab.PuzzleSize.Value = field.Size;
				UI.PuzzleSize = UI.Display.TilesGrid.Columns = field.Size;
			}
		}
		public string CompletionDialogueName => Puzzle.Expected.DialogueName;

		public NonogramContainer UI;

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
			UI = new NonogramContainer(rules, puzzle: this) { Name = "Nonogram", Visible = false }
				.Preset(Control.LayoutPreset.FullRect)
				.SizeFlags(horizontal: Control.SizeFlags.ExpandFill, vertical: Control.SizeFlags.ExpandFill);
			Timer = new() { Provider = this };
			Puzzle = new() { };
			UI.Studio.PuzzleTab.Signals = this;
		}
		public void ClearPuzzle()
		{
			Puzzle.Clear();
			UI.PuzzleSize = Puzzle.Size;
		}

		public void TimeChanged(string value)
		{
			Puzzle.TimeTaken = Timer?.Elapsed ?? TimeSpan.Zero;
			UI.Display.Timer.Time.Text = "[font_size=30]" + value;
		}
		public Node Parent(HintPosition position) => UI.Display.HintsParent(side: position.Side);
		public string Text(HintPosition position) => Puzzle.Expected.States.CalculateHints(position);

		private const TileMode defaultValue = TileMode.Clear;
		public Node Parent() => UI.Display.TilesGrid;
		public TileMode State(Vector2I position)
		{
			return CurrentStates.GetValueOrDefault(position, defaultValue);
		}
		public void OnActivate(Vector2I position, Tile tile)
		{
			Assert(CurrentStates.ContainsKey(position), $"No current tile in the data");

			TileMode mode = PressedMode;
			Tile.Pool tiles = UI.Tiles;
			TileMode current = CurrentStates[position];

			if (!current.IsValidInput(ref mode) || tile.Locked) return;

			Type.InputData(Puzzle).ChangeState(position, mode);
			tile.Mode = mode;
			mode.PlayAudio();

			Type.HandleInput(UI, position);

			switch (Type)
			{
				case Type.Game:
					if (Settings.LineCompleteBlockRest) Puzzle.BlockCompletedLines(tiles, position);
					if (Puzzle.IsComplete) EventHandler?.Completed(Puzzle);
					Timer.TryStart(tile: mode);
					break;
			}

			Save(Puzzle);
		}

		public void ModifyName(string value) => Puzzle = Puzzle with { Name = value };
		public void ModifySize(double value)
		{
			int size = double.ConvertToInteger<int>(value);

			Dictionary<Vector2I, TileMode> newCurrent = Data.CreateTiles(size);
			Dictionary<Vector2I, TileMode> newExpected = Data.CreateTiles(size);

			foreach (Vector2I key in newCurrent.Keys)
			{
				if (!Puzzle.States.TryGetValue(key, out TileMode mode)) continue;
				newCurrent[key] = mode;
			}
			foreach (Vector2I key in newExpected.Keys)
			{
				if (!Puzzle.Expected.States.TryGetValue(key, out TileMode mode)) continue;
				newExpected[key] = mode;
			}

			Puzzle = Puzzle with
			{
				Tiles = newCurrent,
				Expected = Puzzle.Expected with { Tiles = newExpected }
			};
		}
	}
}
