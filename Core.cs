using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;
using Minesweeper;
using Dialogue;
using ConsoleCommand = Console.Console.Command;
using static Console.Console;

public sealed partial class Core : Node
{
	private const string Prefix = "\\";
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
	public static ColourPack Colours
	{
		get
		{
			if (field is not null) return field;
			field = ColourPackPath.LoadOrCreateResource<ColourPack>();
			return field;
		}
	}
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
	public CurrentPuzzle Nonogram => field ??= CreateNonogram();
	private Manager Minesweeper => field ??= CreateMinesweeper();
	public Core()
	{
		_menuHandler = new(this);
		_settingsModifier = new(this);
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
		Nonogram.PuzzleCompleted = OnNonogramPuzzleCompleted;
		Nonogram.SaveListener = _handler;
		Nonogram.UI.Studio.VisibilityChanged += _menuHandler.StudioPuzzleSelectorVisibilityChanged;
		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);

		void ToggleConsole() => Console.Console.Container.Visible = !Console.Console.Container.Visible;
		void OnNonogramPuzzleCompleted(SaveData save)
		{
			Nonogram.UI.CompletionScreen.Show();
			Dialogues.Enable(save.Expected.DialogueName);
		}
		void EscapePressed()
		{
			Container.ShowMainMenu(out bool shown);
			if (shown) return;
			ReadOnlySpan<Control> steps = [Console.Console.Container, Nonogram.UI.CompletionScreen, .. Container.Escapable];
			foreach (Control control in steps)
			{
				if (control.Visible)
				{
					control.Hide();
					Container.Menu.Show();
					Container.Menu.Buttons.Show();
					return;
				}
			}
		}
	}
	public override void _Process(double delta) => Nonogram.Timer.Tick(delta);
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
	private CurrentPuzzle CreateNonogram()
	{
		CurrentPuzzle field = new();
		Container.Add(field.UI);
		field.UI.Colours = Colours;
		field.UI.CompletionScreen.Value.Signals = new PuzzleCompleteScreenHandler(Core: this);
		ConsoleCommand command = new()
		{
			Default = () => field.Type.LogCurrent(),
			Flags = new()
			{
				["game"] = () => (field.Type = Display.Type.Game).LogChange(),
				["paint"] = () => (field.Type = Display.Type.Studio).LogChange(),
			}
		};
		Add(Prefix, ("nonogram", command));
		return field;
	}

	private sealed class PuzzleCompleteScreenHandler(Core Core) : PuzzleCompleteScreen.IHandleSignals
	{
		void PuzzleCompleteScreen.IHandleSignals.OnLevelsPressed()
		{
			Core.Container.Menu.Levels.Show();
			Core.Nonogram.UI.CompletionScreen.Hide();
		}
		void PuzzleCompleteScreen.IHandleSignals.OnDialoguesPressed()
		{
			Core.Container.Menu.Dialogues.Show();
			Core.Nonogram.UI.CompletionScreen.Hide();
		}
		void PuzzleCompleteScreen.IHandleSignals.OnPlayDialoguePressed()
		{
			CurrentPuzzle current = Core.Nonogram;
			var menu = Core.Container.Menu;
			var completionScreen = current.UI.CompletionScreen;
			Dialogues.Start(name: current.CompletionDialogueName);
			menu.Show();
			menu.Background.Hide();
			menu.Buttons.Hide();
			completionScreen.Hide();
		}
		void PuzzleCompleteScreen.IHandleSignals.OnVisibilityChanged()
		{
			CurrentPuzzle current = Core.Nonogram;
			PuzzleCompleteScreen completionScreen = current.UI.CompletionScreen.Value;
			bool visible = completionScreen.Visible;
			string name = current.CompletionDialogueName;
			bool hasDialogue = Dialogues.Contains(name);
			completionScreen.Options.PlayDialogue.Visible = hasDialogue;
			if (hasDialogue)
			{
				completionScreen.Report.Value.Log.Text = "Dialogue: " + name;
			}
		}
	}
	private sealed class MenuHandler(Core Core) : MainMenu.IPress, MainMenu.IReceiveSignals
	{
		public void StudioPuzzleSelectorVisibilityChanged()
		{
			var root = Core.Nonogram.UI.Studio;
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
					void pressed() => Core.Nonogram.StudioPuzzleDisplayPressed(puzzle);
					void OnRightClick(InputEvent input)
					{
						bool rightClicked = Godot.Input.IsMouseButtonPressed(MouseButton.Right);
						if (!rightClicked) return;
						Core.Nonogram.GamePuzzleDisplayPressed(Core.Container.Menu, puzzle);
					}
				}
				puzzles.AddChild(node);
				Core._studioSelectorDisplays.Add(node);
			}
		}

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
			CurrentPuzzle current = Core.Nonogram;
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
		void MainMenu.IPress.OpenStudioPressed()
		{
			CurrentPuzzle current = Core.Nonogram;
			current.Type = Display.Type.Studio;
			current.UI.Show();
			Core.Container.Menu.Hide();
		}

		void MainMenu.IReceiveSignals.PuzzleSelectorVisibilityChanged()
		{
			MainMenu menu = Core.Container.Menu;
			PuzzleSelector value = menu.Levels;
			Container puzzles = value.Puzzles.Value;
			menu.Buttons.Visible = !(menu.Visible = menu.Levels.Visible);
			if (!value.Visible)
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
					void pressed() => Core.Nonogram.GamePuzzleDisplayPressed(menu, puzzle);
				}
				puzzles.AddChild(node);
				Core._levelSelectorDisplays.Add(node);
			}
		}
		void MainMenu.IReceiveSignals.DialogueSelectorVisibilityChanged()
		{
			MainMenu menu = Core.Container.Menu;
			DialogueSelector value = menu.Dialogues;
			VBoxContainer dialogues = value.DisplayContainer.Value;
			menu.Buttons.Visible = !(menu.Visible = value.Visible);
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
			if (nonogram.Visible) { nonogram.Hide(); }
			if (minesweeper.Visible) { minesweeper.Hide(); }
			Node[] visibleChildren = [.. menu.GetChildren()
				.Where(n => n is Control control && control.Visible)
			];

			switch (visibleChildren)
			{
				case []:
					menu.Buttons.Visible = menu.Background.Visible = true;
					break;
				case [MainMenu.MainButtons node] when node == menu.Buttons:
					node.Visible = true;
					break;
				case [ColorRect node] when node == menu.Background:
					node.Visible = true;
					break;
			}
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
			CurrentPuzzle current = Core.Nonogram;
			Hints hints = current.UI.Hints;
			hints.Refresh();
			current.RefreshCurrentStudioIcon(
				colours: Colours,
				displays: Core._studioPuzzleSelectorDisplays
			);
		}
		public void SaveTilesChanged(Vector2I position)
		{
			Display.Type type = Core.Nonogram.Type;
			Nonogram.Tile.Pool tiles = Core.Nonogram.UI.Tiles;
			if (type is not Display.Type.Game) return;
			tiles.TryLock(position);
		}
	}
}