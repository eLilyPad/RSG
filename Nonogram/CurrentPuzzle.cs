using Godot;

namespace RSG.Nonogram;

using static PuzzleData;
using static Display;

public sealed partial class PuzzleManager
{
	public sealed record class CurrentPuzzle : Hints.IProvider, Tile.IProvider
	{
		public IPuzzleEvent? EventHandler { get; set; }
		public Type Type { get; set => UI.Display.Name = (field = value).AsName(); } = Type.Display;
		public Settings Settings { get; set => _playerCompleter.Settings = Timer.Settings = field = value; } = new Settings();
		public PuzzleTimer Timer { get; }
		public string CompletionDialogueName => Puzzle.Expected.DialogueName;
		public SaveData Puzzle
		{
			internal get; set
			{
				if (value is null) { return; }
				Display display = UI.Display;
				Instance.Puzzles[value.Name] = field = value;
				_playerCompleter.Save = _autoCompleter.Save = field;
				Timer.Elapsed = field.TimeTaken;

				UpdateView(field);
				display.TilesGrid.CustomMinimumSize = Mathf.CeilToInt(field.Size) * _hints.TileSize;
			}
		} = new SaveData();

		public NonogramContainer UI => field ??= new NonogramContainer { Name = "Nonogram" }
			.SizeFlags(horizontal: Control.SizeFlags.ExpandFill, vertical: Control.SizeFlags.ExpandFill);

		private readonly Tile.Pool _tiles;
		private readonly Hints _hints;
		private readonly Tile.Locker _tileLocker;
		private readonly SaveData.AutoCompleter _autoCompleter;
		private readonly SaveData.UserInput _playerCompleter;
		internal CurrentPuzzle()
		{
			ColourPack colours = Core.Colours;
			_tileLocker = new(puzzle: this) { };
			Timer = new()
			{
				Settings = Settings,
				TimeChanged = time =>
				{
					Puzzle.TimeTaken = Timer?.Elapsed ?? TimeSpan.Zero;
					UI.Display.Timer.Time.Text = "[font_size=30]" + time;
				}
			};
			_tiles = new(Provider: this, Colours: colours);
			_hints = new(Provider: this, Colours: colours);
			_autoCompleter = new()
			{
				Save = Puzzle,
				Settings = Settings,
				Tiles = _tiles,
			};
			_playerCompleter = new()
			{
				Save = Puzzle,
				Settings = Settings,
				Timer = Timer,
				Tiles = _tiles,
				LockRules = _tileLocker,
				Completer = _autoCompleter,
			};
		}

		public void WhenCodeLoaderEntered(string value)
		{
			RichTextLabel validation = UI.ToolsBar.CodeLoader.Control.Validation;
			//Load(value).Switch(
			//	data => Puzzle = data as SaveData,
			//	error => validation.Text = error.Message,
			//	notFound => GD.Print("Not Found")
			//);
		}
		public void WhenCodeLoaderEdited(string value)
		{
			RichTextLabel validation = UI.ToolsBar.CodeLoader.Control.Validation;
			Code.Encode(value).Switch(
				error => validation.Text = error.Message,
				code => validation.Text = $"valid code of size: {code.Size}"
			);
		}
		Node Hints.IProvider.Parent(HintPosition position)
		{
			Display display = UI.Display;
			return position.Side switch { Side.Row => display.Rows, Side.Column => display.Columns, _ => display };
		}
		string Hints.IProvider.Text(HintPosition position) => Type switch
		{
			Type.Paint => Puzzle.States.CalculateHints(position),
			_ => Puzzle.Expected.States.CalculateHints(position)
		};
		Node Tile.IProvider.Parent() => UI.Display.TilesGrid;
		void Tile.IProvider.OnActivate(Vector2I position, Tile tile)
		{
			if (Type is Type.Game) _playerCompleter.GameInput(position, eventHandler: EventHandler);
			Save(Puzzle);
		}

		private void UpdateView(SaveData save)
		{
			Display display = UI.Display;
			IEnumerable<HintPosition> hintValues = HintPosition.AsRange(display.TilesGrid.Columns = save.Size);
			IEnumerable<Vector2I> tileValues = (Vector2I.One * save.Size).GridRange();

			bool firstTile = true;
			foreach (Vector2I position in tileValues)
			{
				Tile tile = _tiles.GetOrCreate(position);
				IImmutableDictionary<Vector2I, TileMode> expectations = Puzzle.Expected.States, saved = Puzzle.States;

				Assert(expectations.ContainsKey(position), $"No expected tile in the data");
				Assert(saved.ContainsKey(position), $"No current tile in the data");

				TileMode expected = expectations[position], current = saved[position];

				bool
				correctlyFilled = TileMode.Filled.AllEqual(expected, current),
				correctlyBlocked = current is TileMode.Blocked && expected is TileMode.Clear;

				tile.Mode = current;
				tile.Locked = _tileLocker.ShouldLock(position);

				if (firstTile)
				{
					_hints.TileSize = tile.Size;
					firstTile = false;
				}
			}
			foreach (HintPosition position in hintValues)
			{
				Hint hint = _hints.GetOrCreate(position);
				_hints.ApplyText(position, hint);
			}

			_tiles.Clear(exceptions: tileValues);
			_hints.Clear(exceptions: hintValues);
			display.ResetTheme();
		}
	}
}
