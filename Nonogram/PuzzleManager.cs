using Godot;

namespace RSG.Nonogram;

using static PuzzleData;

using static Display;

public interface IHavePuzzleSettings { Settings Settings { get; set; } }
public interface IManagePuzzle
{
	void Completed(SaveData puzzle);
	void SettingsChanged();
}
public sealed partial class PuzzleManager
{
	public sealed record class CurrentPuzzle
	{
		public interface IProvider : Hints.IProvider, Tile.IProvider, PuzzleTimer.IProvider;
		private class Providers : IProvider
		{
			private const string NullCurrent = "No current puzzle present for the provider";
			private const string NullPlayerController = "No player controller present for the provider";
			public Settings Settings { get; set; } = new Settings();
			public CurrentPuzzle Current
			{
				get
				{
					Assert(field is not null, NullCurrent);
					return field;
				}
				set { if (value is not null) field = value; }
			} = null;

			void PuzzleTimer.IProvider.TimeChanged(string value)
			{
				Current.Puzzle.TimeTaken = Current.Timer?.Elapsed ?? TimeSpan.Zero;
				Current.UI.Display.Timer.Time.Text = "[font_size=30]" + value;
			}
			Node Hints.IProvider.Parent(HintPosition position) => Current.UI.Display.HintsParent(side: position.Side);
			string Hints.IProvider.Text(HintPosition position) => Current.Type switch
			{
				Type.Game => Current.Puzzle.Expected.States.CalculateHints(position),
				Type.Paint => Current.Puzzle.States.CalculateHints(position),
				_ => EmptyHint
			};

			const TileMode defaultValue = TileMode.Clear;
			Node Tile.IProvider.Parent() => Current.UI.Display.TilesGrid;
			TileMode Tile.IProvider.State(Vector2I key) => Current.Puzzle.States.GetValueOrDefault(key, defaultValue);
			void Tile.IProvider.OnActivate(Vector2I position, Tile tile)
			{
				Settings settings = Current.Settings;
				SaveData save = Current.Puzzle;
				IManagePuzzle? eventHandler = Current.EventHandler;
				switch (Current.Type)
				{
					case Type.Game:
						Current.PlayerCompleter.GameInput(save, position, tile, settings, eventHandler);
						break;
					case Type.Paint:
						Current.PlayerCompleter.PaintInput(save, position, tile);
						Current.UI.Display.Hints.Refresh();
						break;
				}

				Save(Current.Puzzle);
			}
		}

		public static CurrentPuzzle Create<TEvents>(TEvents events, Node parent, ColourPack colours)
		where TEvents : IManagePuzzle, PuzzleCompleteScreen.IHandleSignals
		{
			Providers provider = new();
			Tile.Pool tiles = new(Provider: provider, Colours: colours);
			Hints hints = new(Provider: provider, Colours: colours);
			Display display = new Default(tiles, hints);
			PuzzleTimer timer = new() { Provider = provider };
			SaveData.AutoCompleter autoCompleter = new() { Tiles = display.Tiles, };
			SaveData.UserInput playerCompleter = new() { Timer = timer, Tiles = display.Tiles, Completer = autoCompleter };

			NonogramContainer ui = new NonogramContainer { Name = "Nonogram", Display = display, Visible = false }
				.Preset(Control.LayoutPreset.FullRect);

			CurrentPuzzle current = new()
			{
				Provider = provider,
				UI = ui,
				AutoCompleter = autoCompleter,
				PlayerCompleter = playerCompleter,
				Timer = timer
			};
			display.Tiles.LockRules.Add(current.Rules);

			parent.AddChild(ui);

			provider.Current = current;
			ui.CompletionScreen.Value.Signals = events;
			ui.Colours = colours;
			Console.Console.Command command = new()
			{
				Default = () => Console.Console.Log("do nothing, show help"),
				Flags = new()
				{
					["toggle_completion"] = () => ui.CompletionScreen.Visible = !ui.CompletionScreen.Visible,
				}
			};
			Console.Console.Add(Core.DefaultCommandPrefix, ("nonogram", command));

			return current;
		}

		public required IPuzzleTimer Timer { get; init; }
		public required NonogramContainer UI
		{
			get; init
			{
				field = value;
				field.Display.Tiles.LockRules.Add([.. Rules]);
			}
		}
		public required IProvider Provider { get; init; }
		public required SaveData.UserInput PlayerCompleter { private get; init; }
		public required SaveData.AutoCompleter AutoCompleter { private get; init; }
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
			get => Provider.Settings; set
			{
				Provider.Settings = value;
				EventHandler?.SettingsChanged();
			}
		}
		public SaveData Puzzle
		{
			get; set
			{
				if (value is null) return;
				field = value;

				Instance.Puzzles[field.Name] = field;
				Timer.Elapsed = field.TimeTaken;
				PuzzleReady = true;
				UI.Display.PuzzleSize = UI.Display.TilesGrid.Columns = field.Size;
			}
		} = new() { };
		public List<Func<Vector2I, bool>> Rules => [
			(position) => Settings.LockCompletedFilledTiles && Puzzle.IsCorrectlyFilled(position),
			(position) => Settings.LockCompletedBlockedTiles && Puzzle.IsCorrectlyBlocked(position),
		];

		private CurrentPuzzle() { }
	}

	private static PuzzleManager Instance => field ??= new();

	public static IEnumerable<PuzzleSelector.PackDisplay.Config> SelectorConfigs => [
		new PuzzleSelector.PackDisplay.Config("Saved Puzzles", GetSavedPuzzles()),
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
