using Godot;

namespace RSG.Nonogram;

using static PuzzleData;

using static Display;
public sealed class PuzzleManager
{
	public sealed record class Settings
	{
		public bool LineCompleteBlockRest { get; set; } = true;
		public bool HaveTimer { get; set; } = true;
	}
	public sealed record class CurrentPuzzle : Hints.IProvider, Tiles.IProvider
	{
		public Type Type { get; set => UI.Display.Name = (field = value).AsName(); } = Type.Display;
		public Settings Settings { get; set; } = new Settings();
		public PuzzleTimer Timer { get; }
		public SaveData Puzzle
		{
			private get; set
			{
				if (value is null) { return; }
				Display display = UI.Display;
				Instance.Puzzles[value.Name] = field = value;
				IEnumerable<HintPosition> hintValues = HintPosition.AsRange(display.TilesGrid.Columns = value.Size);
				IEnumerable<Vector2I> tileValues = (Vector2I.One * value.Size).GridRange();
				for (int i = 0; i < tileValues.Count(); i++)
				{
					Vector2I position = tileValues.ElementAt(i);
					Tile tile = _tiles.GetOrCreate(position);
					TileMode state = value.States.GetValueOrDefault(key: position, defaultValue: TileMode.NULL);
					_tiles.ChangeMode(position, tile, input: state);

					if (i == 0) _hints.TileSize = tile.Size;
				}
				foreach (HintPosition position in hintValues)
				{
					Hint hint = _hints.GetOrCreate(position);
					_hints.ApplyText(position, hint);
				}
				_tiles.Clear(exceptions: tileValues);
				_hints.Clear(exceptions: hintValues);

				Timer.Elapsed = value.TimeTaken;

				float scale = value.Size * value.Size / value.Size;
				display.TilesGrid.CustomMinimumSize = Mathf.CeilToInt(scale) * _hints.TileSize;
				display.ResetTheme();

			}
		} = new SaveData();

		public NonogramContainer UI => field ??= new NonogramContainer { Name = "Nonogram" }
			.SizeFlags(horizontal: Control.SizeFlags.ExpandFill, vertical: Control.SizeFlags.ExpandFill);

		private readonly Tiles _tiles;
		private readonly Hints _hints;
		internal CurrentPuzzle()
		{
			_tiles = new(Provider: this, Colours: Core.Colours);
			_hints = new(Provider: this, Colours: Core.Colours);
			Timer = new()
			{
				TimeChanged = text =>
				{
					Puzzle.TimeTaken = Timer?.Elapsed ?? TimeSpan.Zero;
					UI.Display.Timer.Time.Text = text;
				}
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
		Node Tiles.IProvider.Parent() => UI.Display.TilesGrid;
		string Tiles.IProvider.Text(Vector2I position) => Current.Puzzle.States.AsText(position);
		void Tiles.IProvider.Activate(Vector2I position, Tile tile)
		{
			TileMode input = PressedMode.Change(tile.Button.Text.FromText());
			if (input is TileMode.NULL) return;
			switch (Type)
			{
				case Type.Game:
					ChangeState(position, mode: input, tile);
					if (Settings.LineCompleteBlockRest)
					{
						foreach (Side side in stackalloc[] { Side.Row, Side.Column })
						{
							if (!Puzzle.IsLineComplete(position, side)) { continue; }
							var line = Puzzle.States.AllInLine(position, side, without: TileMode.Filled);
							foreach ((Vector2I coord, TileMode _) in line)
							{
								ChangeState(position: coord, mode: TileMode.Blocked);
							}
						}
					}
					if (Settings.HaveTimer && input is TileMode.Filled && !Timer.Running) Timer.Running = true;
					input.PlayAudio();
					Save(Puzzle);
					if (Puzzle is { IsComplete: true })
					{
						//Instance.PuzzlesCompleted[PuzzleManager.Current.Puzzle.Name] = true;
						UI.CompletionScreen.Show();
						if (Puzzle.Expected.DialogueName is string dialogueName)
						{
							Dialogues.Start(dialogueName, true);
						}

					}
					break;
				case Type.Paint:
					//tile.Button.Text = input == previousMode ? EmptyText : input.AsText();
					//foreach (HintPosition hintPosition in HintPosition.Convert(position))
					//{
					//	if (!Hints.TryGetValue(hintPosition, out Hint? hint)) { continue; }
					//	string hints = Tiles.CalculateHints(hintPosition);
					//	hint.Label.Text = hintPosition.Side is Side.Row ? hints + " " : hints;
					//}
					break;
				default: break;
			}
			void ChangeState(Vector2I position, TileMode mode, Tile? tile = null)
			{
				Puzzle.ChangeState(position, mode);
				_tiles.ChangeMode(position, tile ?? _tiles.GetOrCreate(position), input: mode);
			}
		}
	}

	public static CurrentPuzzle Current => field ??= new();
	internal static PuzzleManager Instance => field ??= new();

	public static IEnumerable<(string Name, IEnumerable<SaveData> Data)> SelectorConfigs => [
		("Saved Puzzles", GetSavedPuzzles()),
		.. GetPuzzlePacks().Select(Pack.Convert)
	];
	public static IReadOnlyList<Pack> GetPuzzlePacks() => [.. Instance.PuzzlePacks];
	public static IList<SaveData> GetSavedPuzzles() => FileManager.GetSaved();
	public static void Save(OneOf<PuzzleData, SaveData> puzzle)
	{
		puzzle.Switch(Puzzle, Savable);
		static void Savable(SaveData save)
		{
			save = save with { Name = save.Name + " save" };
			FileManager.Save(save);
			Instance.Puzzles[save.Name] = save;
		}
		static void Puzzle(PuzzleData data)
		{
			FileManager.Save(data);
			Instance.Puzzles[data.Name] = data;
		}
	}

	public List<Pack> PuzzlePacks { get; } = [Pack.Procedural()];
	public Dictionary<string, bool> PuzzlesCompleted { private get; init; } = [];
	public Dictionary<string, string> CompletionDialogues { private get; init; } = [];
	public Dictionary<string, Data> Puzzles { private get; init; } = new()
	{
		[Data.DefaultName] = new PuzzleData()
	};

	private PuzzleManager() { }
	public void AddPuzzle(SaveData save)
	{
		Puzzles[save.Name] = save;
	}
}
