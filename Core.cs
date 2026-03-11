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
			MainMenu menu = Core.Container.Menu;
			Display.Type type = current.Type;

			(current.UI.Visible, current.Type, menu.Visible, menu.Levels.Visible) = current switch
			{
				{ PuzzleReady: false } => (false, type, true, true),
				{ Type: Display.Type.Studio } => (current.UI.Visible, Display.Type.Game, true, true),
				{ PuzzleReady: true } => (true, type, false, menu.Levels.Visible),
			};
			menu.Buttons.Visible = false;
		}
		void MainMenu.IPress.OpenStudioPressed()
		{
			CurrentPuzzle current = Core.Nonogram;
			current.Type = Display.Type.Studio;
			current.UI.Visible = true;
			Core.Container.Menu.Visible = false;
		}

		public void StudioPuzzleSelectorVisibilityChanged()
		{
			CurrentPuzzle current = Core.Nonogram;
			NonogramStudioBar root = current.UI.Studio;
			VBoxContainer puzzles = root.PacksTab.Scroll.Puzzles;
			MainMenu menu = Core.Container.Menu;
			if (!root.Visible) return;
			puzzles.Remove(true, Core._studioSelectorDisplays);
			Core._studioSelectorDisplays.Clear();
			Core._studioPuzzleSelectorDisplays.Clear();
			foreach ((string Name, IEnumerable<SaveData> Data) config in PuzzleManager.SelectorConfigs)
			{
				PuzzleSelector.PackDisplay node = PuzzleSelector.CreateStudioPack(
					name: config.Name,
					packs: Core._studioSelectorDisplays,
					parent: puzzles
				);
				Control parent = node.Puzzles.Value;

				foreach (SaveData data in config.Data)
				{
					_ = PuzzleSelector.CreateStudioDisplay(
						puzzle: data,
						values: Core._studioPuzzleSelectorDisplays,
						parent,
						menu,
						handler: current
					);
				}
			}
		}

		void MainMenu.IReceiveSignals.PuzzleSelectorVisibilityChanged()
		{
			MainMenu menu = Core.Container.Menu;
			CurrentPuzzle current = Core.Nonogram;
			PuzzleSelector value = menu.Levels;
			Container puzzles = value.Puzzles.Value;

			menu.Buttons.Visible = !(menu.Visible = value.Visible);
			menu.Visible = value.Visible && menu.Visible;

			if (!value.Visible) return;

			puzzles.Remove(true, Core._levelSelectorDisplays);
			Core._levelSelectorDisplays.Clear();
			foreach (var config in PuzzleManager.SelectorConfigs)
			{
				var node = PuzzleSelector.CreateGamePack(
					name: config.Name,
					parent: puzzles,
					packs: Core._levelSelectorDisplays
				);
				Container puzzleParent = node.Puzzles.Value;
				foreach (SaveData puzzle in config.Data)
				{
					_ = PuzzleSelector.CreateGameDisplay(
						puzzle,
						parent: puzzleParent,
						menu,
						handler: current
					);
				}
			}
		}
		void MainMenu.IReceiveSignals.DialogueSelectorVisibilityChanged()
		{
			MainMenu menu = Core.Container.Menu;
			DialogueSelector value = menu.Dialogues;
			VBoxContainer dialogues = value.DisplayContainer.Value;

			menu.Buttons.Visible = !(menu.Visible = value.Visible);
			menu.Visible = value.Visible && menu.Visible;

			if (!value.Visible) return;

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

			nonogram.Visible = !nonogram.Visible && nonogram.Visible;
			minesweeper.Visible = !minesweeper.Visible && minesweeper.Visible;
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
			List<PuzzleSelector.PuzzleDisplay> displays = Core._studioPuzzleSelectorDisplays;
			CurrentPuzzle current = Core.Nonogram;
			string name = current.Name;
			current.UI.Hints.Refresh();
			if (!displays.TryGetByName(name, value: out var display)) return;
			display.Button.Icon = current.StudioIcon(colours: Colours);
		}
		public void SaveTilesChanged(Vector2I position) => Core.Nonogram.TryLockTile(position);
	}
}