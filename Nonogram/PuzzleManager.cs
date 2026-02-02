using Godot;

namespace RSG.Nonogram;

using static PuzzleData;

using static Display;

public interface IHavePuzzleSettings { Settings Settings { get; } }
public interface IManagePuzzle
{
	void Completed(SaveData puzzle);
	void SettingsChanged();
}
public sealed partial class PuzzleManager
{
	public sealed record class CurrentPuzzle : Hints.IProvider, Tile.IProvider, PuzzleTimer.IProvider
	{
		public PuzzleTimer Timer { get; }
		public IManagePuzzle? EventHandler { get; set; }
		public bool PuzzleReady { get; private set; } = false;
		public Type Type
		{
			get; set
			{
				UI.Display.Name = value.AsName();
				UI.Display.Spacer.Type = value;
				field = value;
			}
		} = Type.Game;
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
			get; set
			{
				if (value is null) return;
				field = value;

				Instance.Puzzles[field.Name] = field;
				Timer.Elapsed = field.TimeTaken;
				PuzzleReady = true;
				UI.PuzzleSize = UI.Display.TilesGrid.Columns = field.Size;
			}
		} = new() { };
		public List<Func<Vector2I, bool>> Rules => [
			(position) => Settings.LockCompletedFilledTiles && Puzzle.IsCorrectlyFilled(position),
			(position) => Settings.LockCompletedBlockedTiles && Puzzle.IsCorrectlyBlocked(position),
		];

		public NonogramContainer UI { get; }

		private readonly SaveData.AutoCompleter _autoCompleter;
		private readonly SaveData.UserInput _playerCompleter;
		internal CurrentPuzzle()
		{
			UI = new NonogramContainer(Core.Colours, Rules, this)
			{
				Name = "Nonogram",
				Visible = false
			}.Preset(Control.LayoutPreset.FullRect);
			Timer = new() { Provider = this };
			_autoCompleter = new() { Tiles = UI.Tiles, };
			_playerCompleter = new()
			{
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
		string Hints.IProvider.Text(HintPosition position) => Type switch
		{
			Type.Game => Puzzle.Expected.States.CalculateHints(position),
			Type.Paint => Puzzle.States.CalculateHints(position),
			_ => EmptyHint
		};

		Node Tile.IProvider.Parent() => UI.Display.TilesGrid;
		TileMode Tile.IProvider.State(Vector2I position)
		{
			return Puzzle.States.GetValueOrDefault(position, TileMode.Clear);
		}
		void Tile.IProvider.OnActivate(Vector2I position, Tile tile)
		{
			Settings settings = Settings;
			SaveData save = Puzzle;
			IManagePuzzle? eventHandler = EventHandler;
			switch (Type)
			{
				case Type.Game:
					_playerCompleter.GameInput(save, position, tile, settings, eventHandler);
					break;
				case Type.Paint:
					_playerCompleter.PaintInput(save, position, tile);
					UI.Hints.Refresh();
					break;
			}

			Save(Puzzle);
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
	public Dictionary<string, Data> Puzzles { private get; init; } = new() { [Data.DefaultName] = new PuzzleData() };

	private PuzzleManager() { }
}
