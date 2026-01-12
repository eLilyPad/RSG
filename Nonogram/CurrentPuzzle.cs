using Godot;

namespace RSG.Nonogram;

using static Display;

public interface IHavePuzzleSettings { Settings Settings { get; } }

public sealed partial class PuzzleManager
{
	public sealed record class CurrentPuzzle : Hints.IProvider, Tile.IProvider, PuzzleTimer.IProvider
	{
		private static NonogramContainer Create(CurrentPuzzle puzzle)
		{
			ColourPack colours = Core.Colours;
			List<Func<Vector2I, bool>> rules = [
				(position) => puzzle.Settings.LockCompletedFilledTiles && puzzle.Puzzle.IsCorrectlyFilled(position),
				(position) => puzzle.Settings.LockCompletedBlockedTiles && puzzle.Puzzle.IsCorrectlyBlocked(position),
			];
			return new NonogramContainer(colours, rules, puzzle) { Name = "Nonogram", Visible = false }
				.SizeFlags(horizontal: Control.SizeFlags.ExpandFill, vertical: Control.SizeFlags.ExpandFill);
		}

		public IHaveEvents? EventHandler { get; set; }
		public Type Type { get; set => UI.Display.Name = (field = value).AsName(); } = Type.Display;
		public Settings Settings { get; set => ChangeSettings(field = value); } = new Settings();
		public PuzzleTimer Timer { get; }
		public string CompletionDialogueName => Puzzle.Expected.DialogueName;
		public SaveData Puzzle { private get; set => ChangePuzzle(field = value); } = new SaveData();

		public NonogramContainer UI => field ??= Create(this);

		private readonly SaveData.AutoCompleter _autoCompleter;
		private readonly SaveData.UserInput _playerCompleter;
		internal CurrentPuzzle()
		{
			UI = Create(this);
			Timer = new() { Provider = this };
			_autoCompleter = new() { Save = Puzzle, Tiles = UI.Tiles, };
			_playerCompleter = new()
			{
				Save = Puzzle,
				Settings = Settings,
				Timer = Timer,
				Tiles = UI.Tiles,
				Completer = _autoCompleter,
			};
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
			if (Type is Type.Game) _playerCompleter.GameInput(position, eventHandler: EventHandler);
			Save(Puzzle);
		}

		private void ChangeSettings(Settings value)
		{
			_playerCompleter.Settings = value;
			EventHandler?.SettingsChanged();
		}
		private void ChangePuzzle(SaveData value)
		{
			Assert(value is not null, "null save puzzle was given to the Puzzle manager. Unable to change puzzle");
			Display display = UI.Display;
			Tile.Pool tiles = UI.Tiles;
			Hints hints = UI.Hints;
			int size = display.TilesGrid.Columns = value.Size;

			Instance.Puzzles[value.Name] = value;
			_playerCompleter.Save = _autoCompleter.Save = value;
			Timer.Elapsed = value.TimeTaken;

			tiles.Update(size);
			hints.TileSize = tiles.TileSize;
			hints.Update(size);
			display.ResetTheme();
			display.TilesGrid.CustomMinimumSize = Mathf.CeilToInt(value.Size) * tiles.TileSize;
		}
	}
}
