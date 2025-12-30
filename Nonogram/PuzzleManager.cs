using Godot;

namespace RSG.Nonogram;

using static PuzzleData;

using static Display;

//using LoadResult = OneOf<Display.Data, PuzzleData.Code.ConversionError, NotFound>;

public sealed class PuzzleManager
{
	public sealed record class Settings
	{
		public bool CompleteLineWhenComplete { get; set; } = true;
	}
	public sealed record class CurrentPuzzle : Hints.IProvider, Tiles.IProvider
	{
		public Type Type { get; set => UI.Display.Name = (field = value).AsName(); } = Type.Display;
		public Settings Settings { get; set; } = new Settings();
		public SaveData Puzzle
		{
			get; set
			{
				if (value is null) { return; }
				Instance.Puzzles[value.Name] = field = value;
				Display display = UI.Display;
				IEnumerable<HintPosition> hintValues = HintPosition.AsRange(display.TilesGrid.Columns = value.Size);
				IEnumerable<Vector2I> tileValues = (Vector2I.One * value.Size).GridRange();
				for (int i = 0; i < tileValues.Count(); i++)
				{
					Vector2I position = tileValues.ElementAt(i);
					Tile tile = _tiles.GetOrCreate(position);
					if (value.States.TryGetValue(position, out TileMode state))
					{
						_tiles.ApplyText(position, tile, input: state);
					}
					else
					{
						_tiles.ApplyText(position, tile);
					}
					if (i == 0) _hints.TileSize = tile.Size;
				}
				foreach (HintPosition position in hintValues)
				{
					Hint hint = _hints.GetOrCreate(position);
					_hints.ApplyText(position, hint);
				}
				_tiles.Clear(exceptions: tileValues);
				_hints.Clear(exceptions: hintValues);

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
		}

		///// <summary>
		///// Updates the current puzzle from the display.
		/////  - saves the changes to puzzle.
		/////  - Writes to game display
		///// Checks if the display has StatusBar then  
		///// </summary>
		//public void SaveProgress()
		//{
		//	SaveData data = new(Puzzle, Tiles);
		//	Save(data);
		//	Puzzle = data;
		//}
		//public bool CheckCompletion()
		//{
		//	switch (UI.Displays.CurrentTabDisplay)
		//	{
		//		case GameDisplay game when Puzzle is SaveData save:
		//			if (Instance.PuzzlesCompleted[Current.Puzzle.Name] = save.IsComplete)
		//			{
		//				UI.CompletionScreen.Show();
		//				game.Status.CompletionLabel.Text = StatusBar.PuzzleComplete;
		//				if (save.Expected.DialogueName is string dialogueName)
		//				{
		//					GD.Print("starting dialogue " + dialogueName);
		//					Dialogues.Start(dialogueName, true);
		//				}
		//				return true;
		//			}

		//			game.Status.CompletionLabel.Text = StatusBar.PuzzleIncomplete;
		//			break;
		//	}
		//	return false;
		//}
		//public void Reset()
		//{
		//	Vector2 tileSize = Tiles.First().Value.Size;
		//	switch (Type)
		//	{
		//		case DisplayType.Game:
		//			foreach ((Vector2I _, Tile tile) in Tiles)
		//			{
		//				tile.Button.Text = EmptyText;
		//			}
		//			break;
		//		case DisplayType.Paint:
		//			foreach ((Vector2I _, Tile tile) in Tiles)
		//			{
		//				tile.Button.Text = EmptyText;
		//			}
		//			foreach ((HintPosition _, Hint hint) in Hints)
		//			{
		//				hint.Label.Text = EmptyHint;
		//				hint.CustomMinimumSize = tileSize;
		//			}
		//			break;
		//	}
		//}
		//public void OnDisplayTabChanged(UI.MainMenu menu)
		//{
		//	Display current = UI.Display;
		//	Current.Puzzle = Current.Puzzle;
		//	foreach (Display other in UI.Displays.Tabs.ToList().Except([current]))
		//	{
		//		if (other == current
		//			|| other is not IHaveTools { Tools: PopupMenu otherTools }
		//			|| !menu.HasChild(otherTools)
		//		) continue;
		//		menu.RemoveChild(otherTools);
		//	}
		//	if (current is not IHaveTools { Tools: PopupMenu currentTools }
		//		|| menu.HasChild(currentTools)
		//	)
		//	{
		//		return;
		//	}
		//	menu.AddChild(currentTools);
		//}

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

		public Node HintsParent(HintPosition position)
		{
			Display display = UI.Display;
			return position.Side switch { Side.Row => display.Rows, Side.Column => display.Columns, _ => display };
		}
		public string HintsText(HintPosition position)
		{
			string hints = Type switch
			{
				_ when Puzzle is SaveData save => save.Expected.States.CalculateHints(position),
				_ => Puzzle.States.CalculateHints(position)
			};
			return position.Side is Side.Row ? hints + " " : hints;
		}
		public Node TilesParent() => UI.Display.TilesGrid;
		public string TilesText(Vector2I position)
		{
			return Current.Puzzle switch
			{
				//SaveData save when Current.Type is not Type.Game => save.Expected.States.AsText(position),
				//SaveData save => save.States.AsText(position),
				//PuzzleData puzzle => puzzle.States.AsText(position),
				_ => Current.Puzzle.States.AsText(position)
			};
		}
		public void TileInput(Vector2I position, Tile tile)
		{
			TileMode input = PressedMode;
			if (input is TileMode.Clear) return;
			switch (Type)
			{
				case Type.Game:
					_tiles.ApplyText(position, tile, input);
					input.PlayAudio();
					Puzzle.ChangeState(position, input);
					Save(Puzzle);
					GD.Print("saving");
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
		}
	}

	public static CurrentPuzzle Current => field ??= new();
	private static PuzzleManager Instance => field ??= new();

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
			Instance.Puzzles[save.Name] = save.Expected;
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
}
