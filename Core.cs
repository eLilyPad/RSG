using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;
using Minesweeper;
using Dialogue;

public sealed partial class Core : Node
{
	private sealed class MenuHandler(Core Core) : MainMenu.IPress, MainMenu.IReceiveSignals
	{
		readonly List<PuzzleSelector.PackDisplay> _levelSelectorDisplays = [];
		readonly List<PuzzleSelector.PackDisplay> _studioSelectorDisplays = [];
		readonly List<DialogueSelector.DialogueDisplay> _dialogueSelectorDisplays = [];

		public void PuzzleSelectorVisibilityChanged() => Refill(value: Core.Container.Menu.Levels);
		public void DialogueSelectorVisibilityChanged() => Refill(value: Core.Container.Menu.Dialogues);
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
			switch (current)
			{
				case { PuzzleReady: false }:
					Core.Container.Menu.Levels.Show();
					Core.Container.Menu.Show();
					break;
				default:
					Core.Container.Menu.Hide();
					break;
			}
		}

		private void Refill<T>(T value) where T : Control
		{
			MainMenu menu = Core.Container.Menu;
			if (!value.Visible)
			{
				menu.Hide();
				return;
			}
			switch (value)
			{
				case PuzzleSelector puzzle:
					Refill(configs: PuzzleManager.SelectorConfigs, create: PuzzleSelector.PackDisplay.Create);
					break;
				case PuzzleSelector.Studio puzzle:
					Refill(configs: PuzzleManager.SelectorConfigs, create: PuzzleSelector.PackDisplay.CreateForStudio);
					break;
				case DialogueSelector dialogue:
					Refill(configs: Dialogues.AvailableDialogues, create: DialogueSelector.DialogueDisplay.Create);
					break;
				default:
					GD.PrintErr($"Unhandled refill for type {value.GetType().Name}");
					break;
			}
			void Refill<TConfig, TNode>(IEnumerable<TConfig> configs, Func<TConfig, CanvasItem, TNode> create)
			where TNode : Node
			{
				if (!value.Visible) return;
				(List<Node>, Node) a = value switch
				{
					PuzzleSelector puzzle => ([.. _levelSelectorDisplays], puzzle.Puzzles.Value),
					PuzzleSelector.Studio puzzle => ([.. _studioSelectorDisplays], puzzle.Puzzles),
					DialogueSelector dialogue => ([.. _dialogueSelectorDisplays], dialogue.DisplayContainer.Value),
					_ => throw new NotImplementedException(),
				};
				(List<Node> nodes, Node parent) = a;
				parent.Remove(true, nodes);
				nodes.Clear();
				foreach (TConfig config in configs)
				{
					TNode node = create(config, value);
					parent.AddChild(node);
					nodes.Add(node);
				}
			}
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
	private sealed class GamesHandler(Core Core) : IHandleEvents
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
	}

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
		Dialogues.Instance.BuildDialogues();

		Input.Bind(bindsContainer: Container.Menu.Settings.Input.InputsContainer,
			(Key.Escape, Container.EscapePressed, "Toggle Main Menu"),
			(Key.Backslash, CoreUI.ToggleConsole, "Toggle Console")
		);
		InitConsole(this);
		PuzzleManager.Current.PuzzleCompleted = OnNonogramPuzzleCompleted;
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
}

