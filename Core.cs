using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;
using Minesweeper;
using Dialogue;
using ConsoleCommand = Console.Console.Command;
using static Console.Console;

public static class CoreExtensions
{

}

public sealed partial class Core : Node
{
	public const string Prefix = "\\";
	private static void InitConsole(Core core)
	{
		ConsoleCommand
		quitCommand = new() { Default = () => core.GetTree().Quit() },
		minesweeperCommand = new()
		{
			Flags = new()
			{
				["new"] = () =>
				{
					core.Minesweeper.Puzzle = Manager.Data.CreateRandom(10);
					core.Minesweeper.UI.Show();
					Log("Started new Minesweeper game");
				},
				["uncover_all"] = () =>
				{
					core.Minesweeper.UI.Tiles.ShowAll();
					core.Minesweeper.UI.Show();
					Log("Started new Minesweeper game");
				}
			}
		},
		dialogueCommand = new()
		{
			Default = () => Log("Current Dialogue: " + Dialogues.Container.Visible),
			Flags = new()
			{
				["enable_all"] = () =>
				{
					Dialogues.EnableAll();
					Log("Enabled All Dialogues");
				}
			},
			Properties = new()
			{
				["start"] = obj =>
				{
					if (!TryConvertDialogueName(obj, out string? name)) return;
					Dialogues.Start(name);
					Log($"Started Dialogue: {name}");
				},
				["enable"] = obj =>
				{
					if (!TryConvertDialogueName(obj, out string? name)) return;
					Dialogues.Enable(name);
					Log($"Enabled Dialogue: {name}");
				},
			}
		};
		ReadOnlySpan<(string, ConsoleCommand)> configs = [
			("quit", quitCommand),
			("minesweeper", minesweeperCommand),
			("dialogue", dialogueCommand),
		];
		Add(Prefix, configs);

		static bool TryConvertDialogueName(object obj, [MaybeNullWhen(false)] out string name)
		{
			name = null;
			if (obj is not string value)
			{
				Log("Invalid dialogue name");
				return false;
			}
			if (!Dialogues.Contains(value))
			{
				Log("Dialogue does not exist");
				return false;
			}
			name = value;
			return true;
		}
	}

	public const string
	ColourPackPath = "res://Data/DefaultColours.tres",
	MinesweeperTexturesPath = "res://Data/MinesweeperTextures.tres",
	DialoguesPath = "res://Data/Dialogues.tres";
	public static ColourPack Colours => field ?? ColourPackPath.LoadOrCreateResource<ColourPack>();
	public CoreUI Container => field ??= CoreUI.Create(parent: this)
		.SetSettings(_settingsModifier)
		.SetMenu(_menuHandler);
	private readonly SettingsModifier _settingsModifier;
	private readonly MenuHandler _menuHandler;
	private readonly GamesHandler _handler;
	private readonly PuzzleCompleteScreenHandler _nonogramCompleteScreenHandler;
	private readonly List<DialogueSelector.DialogueDisplay> _dialogueSelectorDisplays = [];
	private readonly LevelSelectorDisplays _levelSelectorDisplays;
	private readonly StudioSelectorDisplays _studioSelectorDisplayPool;
	public CurrentPuzzle Nonogram => _nonogram ??= CurrentPuzzle.Create(parent: Container)
		.SetColours(colours: Colours)
		.AddCommands()
		.ConnectVisibilityHandler(handler: _menuHandler)
		.ConnectSignals(puzzleCompletionHandler: _nonogramCompleteScreenHandler, saveListener: _handler);
	private Manager Minesweeper => field ??= CreateMinesweeper();
	private CurrentPuzzle? _nonogram;
	private CoreUI? _container;
	public Core()
	{
		_settingsModifier = new(this);
		_menuHandler = new(this);
		_nonogramCompleteScreenHandler = new(this);
		_levelSelectorDisplays = new(this);
		_studioSelectorDisplayPool = new(this);
		_handler = new(this);
	}
	public override void _Ready()
	{
		Name = nameof(Core);
		Dialogues.Instance.BuildDialogues();

		Input.Bind(bindsContainer: Container.Menu.Settings.Input.InputsContainer,
			(Key.Escape, EscapePressed, "Toggle Main Menu"),
			(Key.Backslash, ToggleConsole, "Toggle Console")
		);
		InitConsole(this);
		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		//Nonogram.ChangePuzzleSize(Display.Data.DefaultSize);

		void ToggleConsole() => Console.Console.Container.Visible = !Console.Console.Container.Visible;
		void EscapePressed()
		{
			ReadOnlySpan<Control> steps = [
				Console.Console.Container,
				Nonogram.UI.CompletionScreen,
				Nonogram.UI,
				Minesweeper.UI,
				Container.Menu.Settings,
				Container.Menu.Levels,
				Container.Menu.Dialogues,
			];
			Container.ShowMainMenu(out bool shown);
			if (shown) return;
			foreach (Control control in steps)
			{
				if (!control.Visible) continue;
				control.Hide();
				Container.Menu.Show();
				Container.Menu.Buttons.Show();
				return;
			}
		}
	}
	public override void _Process(double delta) => _nonogram?.Timer.Tick(delta);
	public override void _Input(InputEvent input)
	{
		if (!input.IsPressed()) return;
		if (Container.LoadingScreen.Visible)
		{
			Container.LoadingScreen.Hide();
			return;
		}
		if (input is InputEventMouseButton { Pressed: true })
		{
			Dialogues.Next(finished: Container.ShowMainMenu);
			return;
		}
		Input.RunEvent(input);
	}

	private Manager CreateMinesweeper()
	{
		MinesweeperContainer ui = new MinesweeperContainer(Colours)
		{
			Name = "Minesweeper",
			Visible = false,
		}.Preset(LayoutPreset.FullRect);
		Manager minesweeper = new() { UI = ui, EventHandler = _handler };

		Container.AddChild(ui);
		ui.Tiles.Provider = minesweeper;

		ui.Resized += () => ui.Background.Border.TextureBorder((Vector2I)ui.Size);
		ui.CompletionScreen.Value.Options.MainMenu.Pressed += () =>
		{
			ui.CompletionScreen.Hide();
			ui.Hide();
			Container.Menu.Show();
		};

		return minesweeper;
	}

	private void ReplaceNonogramCompletionScreen(CanvasItem toShow)
	{
		Nonogram.UI.CompletionScreen.Visible = !(toShow.Visible = true);
	}

	private static void ReplaceVisibility(ReadOnlySpan<CanvasItem> toHide, ReadOnlySpan<CanvasItem> toShow)
	{
		foreach (CanvasItem item in toHide) item.Hide();
		foreach (CanvasItem item in toShow) item.Show();
	}

	private class Displays<T, TPack, TDisplay>(T handler, MainMenu menu, Node parent)
	: NodePool<string, TPack>.PooledGrand<TDisplay>(parent),
		PuzzleSelector.Display.IConfigure<TDisplay>
	where T : PuzzleSelector.Display.IPressed, IIconize
	where TPack : PuzzleSelector.PackDisplay, new()
	where TDisplay : PuzzleSelector.Display, new()
	{
		public TDisplay Configure(TDisplay display, PuzzleData puzzle) => display
			.ChangePuzzle(save: puzzle)
			.ChangeInput(puzzle, menu, handler);
		public void RefreshIcon(string puzzleName, string packName = PuzzleManager.SavedPackName)
		{
			Assert(_puzzleDisplays.ContainsKey(packName), $"Pack '{packName}' not found in pack display pool.");
			if (!_puzzleDisplays[packName].TryGetByName(name: puzzleName, value: out var display)) return;
			display.Button.Icon = handler.ToIcon(colours: Colours);
		}
		public void Load(IEnumerable<PuzzleData.Pack>? configs = null)
		{
			configs ??= PuzzleManager.SelectorConfigs;
			foreach (PuzzleData.Pack data in configs)
			{
				IList<TDisplay> displays = GetGrandChildren(data.Name);
				foreach ((int i, PuzzleData puzzle) in data.Puzzles.Index())
				{
					if (displays.Count <= i) displays.Add(Configure(CreateDisplay(puzzle), puzzle));
					else Configure(displays[i], puzzle);
				}
				return;
			}
		}
		protected override TPack Create(string key)
		{
			TPack pack = new TPack() { Name = key }
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
			Parent(key).AddChild(pack);
			return pack;
		}
		private TDisplay CreateDisplay(PuzzleData puzzle)
		{
			TDisplay display = new() { Name = puzzle.Name };
			DisplaysParent(puzzle).AddChild(display);
			return display;
		}
		private Container DisplaysParent(PuzzleData puzzle) => GetOrCreate(puzzle.Name).Puzzles.Value;
	}
	private sealed class LevelSelectorDisplays(Core Core)
	: Displays<CurrentPuzzle, PuzzleSelector.PackDisplay.Game, PuzzleSelector.Display.Game>(
		handler: Core.Nonogram,
		menu: Core.Container.Menu,
		parent: Core.Container.Menu.Levels.Puzzles.Value
	);
	private sealed class StudioSelectorDisplays(Core Core)
	: Displays<CurrentPuzzle, PuzzleSelector.PackDisplay.Studio, PuzzleSelector.Display.Studio>(
		handler: Core.Nonogram,
		menu: Core.Container.Menu,
		parent: Core.Nonogram.UI.Studio.PacksTab.Scroll.Puzzles
	);
	private sealed class PuzzleCompleteScreenHandler(Core Core) : PuzzleCompleteScreen.IHandleSignals
	{
		private CurrentPuzzle Puzzle => Core.Nonogram;
		private MainMenu Menu => Core.Container.Menu;

		public void OnLevelsPressed() => Core.ReplaceNonogramCompletionScreen(toShow: Menu.Levels);
		public void OnDialoguesPressed() => Core.ReplaceNonogramCompletionScreen(toShow: Menu.Dialogues);
		public void OnPlayDialoguePressed()
		{
			Dialogues.Start(name: Puzzle.CompletionDialogueName);
			ReplaceVisibility([Menu.Background, Menu.Buttons, Puzzle.UI.CompletionScreen, Puzzle.UI], [Menu.Dialogues]);
		}
		public void OnVisibilityChanged()
		{
			bool hasDialogue = Dialogues.Contains(Puzzle.CompletionDialogueName);
			Puzzle.UI.CompletionScreen.Value.Options.PlayDialogue.Visible = hasDialogue;
		}
	}
	private sealed class MenuHandler(Core Core) :
		MainMenu.IPress,
		MainMenu.IReceiveSignals,
		IHandleStudioSelector
	{
		void MainMenu.IPress.LevelsPressed() => Core.Container.Menu.Levels.Show();
		void MainMenu.IPress.DialoguesPressed() => Core.Container.Menu.Dialogues.Show();
		void MainMenu.IPress.SettingsPressed() => Core.Container.Menu.Settings.Show();
		void MainMenu.IPress.QuitPressed() => Core.GetTree().Quit();
		void MainMenu.IPress.PlayMinesweeperPressed()
		{
			Core.Minesweeper.Puzzle = Manager.Data.CreateRandom(10);
			Core.Minesweeper.UI.Show();
			Core.Container.Menu.Hide();
		}
		void MainMenu.IPress.PlayPressed()
		{
			var current = Core.Nonogram;
			var menu = Core.Container.Menu;
			var type = current.Type;

			current.Type = type is Display.Type.Studio ? Display.Type.Game : type;
			current.UI.Visible = current.PuzzleReady;
			menu.Visible = menu.Levels.Visible = !current.PuzzleReady;
			menu.Buttons.Visible = false;
		}
		void MainMenu.IPress.OpenStudioPressed()
		{
			var current = Core.Nonogram;
			current.Type = Display.Type.Studio;
			current.UI.Visible = true;
			Core.Container.Menu.Visible = false;
		}

		void IHandleStudioSelector.StudioPuzzleSelectorVisibilityChanged() => Core._studioSelectorDisplayPool.Load();
		void MainMenu.IReceiveSignals.PuzzleSelectorVisibilityChanged() => Core._levelSelectorDisplays.Load();
		void MainMenu.IReceiveSignals.DialogueSelectorVisibilityChanged()
		{
			MainMenu menu = Core.Container.Menu;
			DialogueSelector value = menu.Dialogues;

			menu.Buttons.Visible = !(menu.Visible = value.Visible);
			menu.Visible = value.Visible && menu.Visible;

			value.Refill(
				parent: value.DisplayContainer.Value,
				nodes: Core._dialogueSelectorDisplays,
				configs: Dialogues.AvailableDialogues,
				create: DialogueSelector.DialogueDisplay.Create
			);
		}
		void MainMenu.IReceiveSignals.SettingsVisibilityChanged()
		{
			MainMenu menu = Core.Container.Menu;
			MainMenu.SettingsContainer value = menu.Settings;
			menu.Buttons.Visible = !(menu.Visible = value.Visible);
		}
		void MainMenu.IReceiveSignals.MenuVisibilityChanged()
		{
			NonogramContainer nonogram = Core.Nonogram.UI;
			MinesweeperContainer minesweeper = Core.Minesweeper.UI;
			MainMenu menu = Core.Container.Menu;

			if (!menu.Visible) { return; }

			Node[] visibleChildren = [.. menu.GetChildren()
				.Where(n => n is Control control && control.Visible)
			];

			(menu.Buttons.Visible, menu.Background.Visible) = visibleChildren switch
			{
				[] => (true, true),
				[ColorRect n] when n == menu.Background => (menu.Buttons.Visible, true),
				[MainMenu.MainButtons n] when n == menu.Buttons => (true, menu.Background.Visible),
				_ => (menu.Buttons.Visible, menu.Background.Visible)
			};
		}
	}
	private sealed class SettingsModifier(Core Core) : SettingsMenuContainer.IChangeSettings, PuzzleManager.IChangeWithSettings
	{
		public void ToggledLockFilledTiles(bool toggled)
		{
			CurrentPuzzle current = Core.Nonogram;
			current.Settings = current.Settings with { LockCompletedFilledTiles = toggled };
		}
		public void ToggledLockBlockedTiles(bool toggled)
		{
			CurrentPuzzle current = Core.Nonogram;
			current.Settings = current.Settings with { LockCompletedBlockedTiles = toggled };
		}
		public void ToggledBlockCompleteLines(bool toggled)
		{
			CurrentPuzzle current = Core.Nonogram;
			current.Settings = current.Settings with { LineCompleteBlockRest = toggled };
		}
		public void SettingsChanged()
		{
			SettingsMenuContainer menu = Core.Container.Menu.Settings.Nonogram;
			Settings settings = Core.Nonogram.Settings;

			menu.AutoCompletion.LockFilledTiles.Value.ButtonPressed = settings.LockCompletedFilledTiles;
			menu.AutoCompletion.LockBlockedTiles.Value.ButtonPressed = settings.LockCompletedBlockedTiles;
			menu.AutoCompletion.BlockCompleteLines.Value.ButtonPressed = settings.LineCompleteBlockRest;
		}
	}
	private sealed class GamesHandler(Core Core) : IHandleEvents, ISaveListener
	{
		void IHandleEvents.Failed(Manager.Data data)
		{
			Backgrounded<MinesweeperContainer.CompletedScreen> completionScreen = Core.Minesweeper.UI.CompletionScreen;
			(completionScreen.Visible, completionScreen.Value.TitleText) = (true, "Game Over");
		}
		void IHandleEvents.Completed(Manager.Data data)
		{
			Backgrounded<MinesweeperContainer.CompletedScreen> completionScreen = Core.Minesweeper.UI.CompletionScreen;
			(completionScreen.Visible, completionScreen.Value.TitleText) = (true, "Mines Located!");
		}
		void ISaveListener.PuzzleTilesChanged(Vector2I position)
		{
			Core.Nonogram.RefreshHints();
			Core._studioSelectorDisplayPool.RefreshIcon(puzzleName: Core.Nonogram.Name);
		}
		void ISaveListener.SaveTilesChanged(Vector2I position) => Core.Nonogram.Tiles.TryLock(position);
	}
}