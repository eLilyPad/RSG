using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;
using Minesweeper;
using Dialogue;

public sealed partial class Core : Node
{
	private static void InitConsole(Core core)
	{
		Console.Console.Command
		quitCommand = new() { Default = () => core.GetTree().Quit() },
		minesweeperCommand = new()
		{
			Flags = new()
			{
				["new"] = () =>
				{
					core.Minesweeper.Puzzle = Manager.Data.CreateRandom(10);
					core.Minesweeper.UI.Show();
					Console.Console.Log("Started new Minesweeper game");
				},
				["uncover_all"] = () =>
				{
					core.Minesweeper.UI.Tiles.ShowAll();
					core.Minesweeper.UI.Show();
					Console.Console.Log("Started new Minesweeper game");
				}
			}
		},
		dialogueCommand = new()
		{
			Default = () => Console.Console.Log("Current Dialogue: " + Dialogues.Container.Visible),
			Flags = new()
			{
				["enable_all"] = () =>
				{
					Dialogues.EnableAll();
					Console.Console.Log("Enabled All Dialogues");
				}
			},
			Properties = new()
			{
				["start"] = obj =>
				{
					if (!TryConvertDialogueName(obj, out string? name)) return;
					Dialogues.Start(name);
					Console.Console.Log($"Started Dialogue: {name}");
				},
				["enable"] = obj =>
				{
					if (!TryConvertDialogueName(obj, out string? name)) return;
					Dialogues.Enable(name);
					Console.Console.Log($"Enabled Dialogue: {name}");
				},
			}
		},
		nonogramCommand = new()
		{
			Default = () => PuzzleManager.Current.Type.LogCurrent(),
			Flags = new()
			{
				["game"] = () => (PuzzleManager.Current.Type = Display.Type.Game).LogChange(),
				["paint"] = () => (PuzzleManager.Current.Type = Display.Type.Studio).LogChange(),
			}
		};
		ReadOnlySpan<(string, Console.Console.Command)> configs = [
			("quit", quitCommand),
			("minesweeper", minesweeperCommand),
			("dialogue", dialogueCommand),
			("nonogram", nonogramCommand)
		];
		Console.Console.Add("\\", configs);

		static bool TryConvertDialogueName(object obj, [MaybeNullWhen(false)] out string name)
		{
			name = null;
			if (obj is not string value)
			{
				Console.Console.Log("Invalid dialogue name");
				return false;
			}
			if (!Dialogues.Contains(value))
			{
				Console.Console.Log("Dialogue does not exist");
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
	public static ColourPack Colours => field ??= ColourPackPath.LoadOrCreateResource<ColourPack>();
	public CoreUI Container => field ??= CoreUI.Create(
		parent: this,
		colours: Colours,
		menu: _menuHandler,
		settings: _settingsModifier
	);
	private readonly SettingsModifier _settingsModifier;
	private readonly MenuHandler _menuHandler;
	private readonly GamesHandler _handler;
	private readonly List<PuzzleSelector.PackDisplay> _levelSelectorDisplays = [];
	private readonly List<PuzzleSelector.PackDisplay> _studioSelectorDisplays = [];
	private readonly List<PuzzleSelector.PuzzleDisplay> _studioPuzzleSelectorDisplays = [];
	private readonly List<DialogueSelector.DialogueDisplay> _dialogueSelectorDisplays = [];
	private Manager Minesweeper
	{
		get
		{
			if (field is not null) return field;
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

			return field = minesweeper;
		}
	}
	public Core()
	{
		_menuHandler = new(this);
		_settingsModifier = new(this);
		_handler = new(this);
	}
	public override void _Ready()
	{
		Name = nameof(Core);
		var current = PuzzleManager.Current;
		Dialogues.Instance.BuildDialogues();

		Input.Bind(bindsContainer: Container.Menu.Settings.Input.InputsContainer,
			(Key.Escape, Container.EscapePressed, "Toggle Main Menu"),
			(Key.Backslash, CoreUI.ToggleConsole, "Toggle Console")
		);
		InitConsole(this);
		current.PuzzleCompleted = OnNonogramPuzzleCompleted;
		current.SaveListener = _handler;
		current.UI.Studio.VisibilityChanged += _menuHandler.StudioPuzzleSelectorVisibilityChanged;
		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);

		static void OnNonogramPuzzleCompleted(SaveData save)
		{
			PuzzleManager.Current.UI.CompletionScreen.Show();
			Dialogues.Enable(save.Expected.DialogueName);
		}
	}
	public override void _Process(double delta)
	{
		PuzzleManager.Current.Timer.Tick(delta);
	}
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
			Dialogues.Next(finished: DialogueFinished);
			return;
		}
		Input.RunEvent(input);

		void DialogueFinished() => Container.Menu.Show();
	}

	private sealed class MenuHandler(Core Core) : MainMenu.IPress, MainMenu.IReceiveSignals
	{
		public void StudioPuzzleSelectorVisibilityChanged()
		{
			var root = PuzzleManager.Current.UI.Studio;
			var puzzles = root.PacksTab.Scroll.Puzzles;
			if (!root.Visible) return;
			puzzles.Remove(true, Core._studioSelectorDisplays);
			Core._studioSelectorDisplays.Clear();
			Core._studioPuzzleSelectorDisplays.Clear();
			foreach ((string Name, IEnumerable<SaveData> Data) config in PuzzleManager.SelectorConfigs)
			{
				var node = PuzzleSelector.PackDisplay.CreateForStudio(config);
				foreach (SaveData puzzle in config.Data)
				{
					var child = PuzzleSelector.PuzzleDisplay.CreateStudioDisplay(puzzle, pressed);
					node.Puzzles.Value.Add(child);
					Core._studioPuzzleSelectorDisplays.Add(child);
					child.Button.GuiInput += OnRightClick;
					void pressed() => PuzzleManager.Current.StudioPuzzleDisplayPressed(puzzle);
					void OnRightClick(InputEvent input)
					{
						bool rightClicked = Godot.Input.IsMouseButtonPressed(MouseButton.Right);
						if (!rightClicked) return;
						PuzzleManager.Current.GamePuzzleDisplayPressed(Core.Container.Menu, puzzle);
					}
				}
				puzzles.AddChild(node);
				Core._studioSelectorDisplays.Add(node);
			}
		}
		public void PuzzleSelectorVisibilityChanged()
		{
			var menu = Core.Container.Menu;
			var selector = menu.Levels;
			var puzzles = selector.Puzzles.Value;
			if (!selector.Visible)
			{
				menu.Hide();
				return;
			}
			puzzles.Remove(true, Core._levelSelectorDisplays);
			Core._levelSelectorDisplays.Clear();
			foreach (var config in PuzzleManager.SelectorConfigs)
			{
				var node = PuzzleSelector.PackDisplay.CreateForGame(config);
				foreach (SaveData puzzle in config.Data)
				{
					var child = PuzzleSelector.PuzzleDisplay.CreateGameDisplay(puzzle, pressed);
					node.Puzzles.Value.Add(child);
					void pressed() => PuzzleManager.Current.GamePuzzleDisplayPressed(menu, puzzle);
				}
				puzzles.AddChild(node);
				Core._levelSelectorDisplays.Add(node);
			}
		}
		public void DialogueSelectorVisibilityChanged()
		{
			var menu = Core.Container.Menu;
			var value = menu.Dialogues;
			var dialogues = value.DisplayContainer.Value;
			if (!value.Visible)
			{
				menu.Hide();
				return;
			}
			dialogues.Remove(true, Core._dialogueSelectorDisplays);
			Core._dialogueSelectorDisplays.Clear();
			foreach (var config in Dialogues.AvailableDialogues)
			{
				var node = DialogueSelector.DialogueDisplay.Create(config, value);
				dialogues.AddChild(node);
				Core._dialogueSelectorDisplays.Add(node);
			}
		}
		public void LevelsPressed() => Core.Container.Menu.Levels.Show();
		public void DialoguesPressed() => Core.Container.Menu.Dialogues.Show();
		public void SettingsPressed() => Core.Container.Menu.Settings.Show();
		public void QuitPressed() => Core.GetTree().Quit();
		public void MenuVisibilityChanged()
		{
			NonogramContainer nonogram = PuzzleManager.Current.UI;
			MinesweeperContainer minesweeper = Core.Minesweeper.UI;
			if (!Core.Container.Menu.Visible) { return; }
			if (nonogram.Visible) { nonogram.Hide(); }
			if (minesweeper.Visible) { minesweeper.Hide(); }
		}
		public void PlayMinesweeperPressed()
		{
			Core.Minesweeper.Puzzle = Manager.Data.CreateRandom(10);
			Core.Minesweeper.UI.Show();
			Core.Container.Menu.Hide();
		}
		public void PlayPressed()
		{
			CurrentPuzzle current = PuzzleManager.Current;
			var menu = Core.Container.Menu;
			switch (current)
			{
				case { Type: Display.Type.Studio }:
					current.Type = Display.Type.Game;
					menu.Levels.Show();
					menu.Show();
					break;
				case { PuzzleReady: true }:
					menu.Hide();
					current.UI.Show();
					break;
				case { PuzzleReady: false }:
					menu.Levels.Show();
					menu.Show();
					break;
				default:
					break;
			}
			Core.Container.Menu.Buttons.Hide();
		}
		public void OpenStudioPressed()
		{
			CurrentPuzzle current = PuzzleManager.Current;
			current.Type = Display.Type.Studio;
			current.UI.Show();
			Core.Container.Menu.Hide();
		}
	}
	private sealed class SettingsModifier(Core Core) : SettingsMenuContainer.IChangeSettings, PuzzleManager.IChangeWithSettings
	{
		public void ToggledLockFilledTiles(bool toggled)
		{
			CurrentPuzzle current = PuzzleManager.Current;
			current.Settings = current.Settings with { LockCompletedFilledTiles = toggled };
		}
		public void ToggledLockBlockedTiles(bool toggled)
		{
			CurrentPuzzle current = PuzzleManager.Current;
			current.Settings = current.Settings with { LockCompletedBlockedTiles = toggled };
		}
		public void ToggledBlockCompleteLines(bool toggled)
		{
			CurrentPuzzle current = PuzzleManager.Current;
			current.Settings = current.Settings with { LineCompleteBlockRest = toggled };
		}
		public void SettingsChanged()
		{
			SettingsMenuContainer menu = Core.Container.Menu.Settings.Nonogram;
			Settings settings = PuzzleManager.Current.Settings;

			menu.AutoCompletion.LockFilledTiles.Value.ButtonPressed = settings.LockCompletedFilledTiles;
			menu.AutoCompletion.LockBlockedTiles.Value.ButtonPressed = settings.LockCompletedBlockedTiles;
			menu.AutoCompletion.BlockCompleteLines.Value.ButtonPressed = settings.LineCompleteBlockRest;
		}
	}
	private sealed class GamesHandler(Core Core) : IHandleEvents, ISaveListener
	{
		public void Failed(Manager.Data data)
		{
			Backgrounded<MinesweeperContainer.CompletedScreen> completionScreen = Core.Minesweeper.UI.CompletionScreen;
			completionScreen.Show();
			completionScreen.Value.TitleText = "Game Over";
		}
		public void Completed(Manager.Data data)
		{
			Backgrounded<MinesweeperContainer.CompletedScreen> completionScreen = Core.Minesweeper.UI.CompletionScreen;
			completionScreen.Show();
			completionScreen.Value.TitleText = "Mines Located!";
		}
		public void PuzzleTilesChanged(Vector2I position)
		{
			CurrentPuzzle current = PuzzleManager.Current;
			Hints hints = current.UI.Hints;
			hints.Refresh();
			current.RefreshCurrentStudioIcon(
				colours: Colours,
				displays: Core._studioPuzzleSelectorDisplays
			);
		}
		public void SaveTilesChanged(Vector2I position)
		{
			Display.Type type = PuzzleManager.Current.Type;
			Nonogram.Tile.Pool tiles = PuzzleManager.Current.UI.Tiles;
			if (type is not Display.Type.Game) return;
			tiles.TryLock(position);
		}
	}
}