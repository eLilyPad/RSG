using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;
using Minesweeper;
using Dialogue;
using RSG.Console;

public sealed partial class Core : Node
{
	private sealed class NonogramEvents(Core core) : IManagePuzzle, PuzzleCompleteScreen.IHandleSignals
	{
		public void OnLevelsPressed()
		{
			core.Container.Menu.Show();
			core.Container.Menu.Levels.Show();
			core.Nonogram.UI.CompletionScreen.Hide();
		}
		public void OnDialoguesPressed()
		{
			core.Container.Menu.Show();
			core.Container.Menu.Dialogues.Show();
			core.Nonogram.UI.CompletionScreen.Hide();
		}
		public void OnPlayDialoguePressed()
		{
			CurrentPuzzle current = core.Nonogram;
			Dialogues.Start(name: current.Puzzle.Expected.DialogueName);
			current.UI.CompletionScreen.Hide();
			current.UI.Hide();
		}
		public void OnVisibilityChanged()
		{
			CurrentPuzzle current = core.Nonogram;
			PuzzleCompleteScreen completionScreen = current.UI.CompletionScreen.Value;
			string name = current.Puzzle.Expected.DialogueName;
			bool hasDialogue = Dialogues.Contains(name);
			completionScreen.Options.PlayDialogue.Visible = hasDialogue;
			if (hasDialogue)
			{
				completionScreen.Report.Value.Log.Text = "Dialogue: " + name;
			}
		}

		public void Completed(SaveData puzzle)
		{
			string dialogueName = puzzle.Expected.DialogueName;
			core.Nonogram.UI.CompletionScreen.Show();
			Dialogues.Enable(dialogueName);
		}
		public void SettingsChanged()
		{
			SettingsMenuContainer menu = core.Container.Menu.Settings.Nonogram;
			Settings settings = core.Nonogram.Settings;

			menu.AutoCompletion.LockFilledTiles.Value.ButtonPressed = settings.LockCompletedFilledTiles;
			menu.AutoCompletion.LockBlockedTiles.Value.ButtonPressed = settings.LockCompletedBlockedTiles;
			menu.AutoCompletion.BlockCompleteLines.Value.ButtonPressed = settings.LineCompleteBlockRest;
		}
	}
	private sealed class EventHandler(Core core) :
	IHandleEvents,
	MainMenuButtons.IPress,
	MainMenu.IReceiveSignals,
	SettingsMenuContainer.IChangeSettings
	{
		readonly List<PuzzleSelector.PackDisplay> _levelSelectorDisplays = [];
		readonly List<DialogueSelector.DialogueDisplay> _dialogueSelectorDisplays = [];
		public void PuzzleSelectorVisibilityChanged() => Refill(value: core.Container.Menu.Levels);
		public void DialogueSelectorVisibilityChanged() => Refill(value: core.Container.Menu.Dialogues);
		public void MenuVisibilityChanged()
		{
			NonogramContainer nonogram = core.Nonogram.UI;
			MinesweeperContainer minesweeper = core.Minesweeper.UI;
			if (!core.Container.Menu.Visible) { return; }
			if (nonogram.Visible) { nonogram.Hide(); }
			if (minesweeper.Visible) { minesweeper.Hide(); }
		}
		public void PlayMinesweeperPressed()
		{
			core.Minesweeper.Puzzle = Manager.Data.CreateRandom(10);
			core.Minesweeper.UI.Show();
			core.Container.Menu.Hide();
		}
		public void PlayPressed()
		{
			CurrentPuzzle current = core.Nonogram;
			if (current.PuzzleReady)
			{
				core.Container.Menu.Hide();
				current.UI.Show();
				current.Type = PuzzleManager.Type.Game;
			}
			else
			{
				core.Container.Menu.Levels.Show();
				core.Container.Menu.Show();
			}
			core.Container.Menu.Buttons.Hide();
		}
		public void StudioPressed()
		{
			CurrentPuzzle current = core.Nonogram;

			core.Container.Menu.Hide();
			core.Container.Menu.Buttons.Hide();

			current.UI.Show();
			current.Type = PuzzleManager.Type.Paint;
			current.Puzzle = new(new(10));
		}
		public void LevelsPressed()
		{
			CurrentPuzzle current = core.Nonogram;
			core.Container.Menu.Levels.Show();
			current.Type = PuzzleManager.Type.Game;
		}
		public void DialoguesPressed() => core.Container.Menu.Dialogues.Show();
		public void SettingsPressed() => core.Container.Menu.Settings.Show();
		public void QuitPressed() => core.GetTree().Quit();
		public void ToggledLockFilledTiles(bool toggled)
		{
			CurrentPuzzle current = core.Nonogram;
			current.Settings = current.Settings with { LockCompletedFilledTiles = toggled };
		}
		public void ToggledLockBlockedTiles(bool toggled)
		{
			CurrentPuzzle current = core.Nonogram;
			current.Settings = current.Settings with { LockCompletedBlockedTiles = toggled };
		}
		public void ToggledBlockCompleteLines(bool toggled)
		{
			CurrentPuzzle current = core.Nonogram;
			current.Settings = current.Settings with { LineCompleteBlockRest = toggled };
		}

		public void Failed(Manager.Data data)
		{
			Backgrounded<MinesweeperContainer.CompletedScreen> completionScreen = core.Minesweeper.UI.CompletionScreen;
			completionScreen.Show();
			completionScreen.Value.TitleText = "Game Over";
		}
		public void Completed(Manager.Data data)
		{
			Backgrounded<MinesweeperContainer.CompletedScreen> completionScreen = core.Minesweeper.UI.CompletionScreen;
			completionScreen.Show();
			completionScreen.Value.TitleText = "Mines Located!";
		}

		private void Refill<T>(T value) where T : Control
		{
			MainMenu menu = core.Container.Menu;
			if (!value.Visible)
			{
				menu.Hide();
				return;
			}
			switch (value)
			{
				case PuzzleSelector puzzle:
					var current = core.Nonogram;
					puzzle.Refill(
						parent: puzzle.Puzzles.Value,
						nodes: _levelSelectorDisplays,
						configs: PuzzleManager.SelectorConfigs,
						create: PuzzleSelector.PackDisplay.Create(current)
					);
					break;
				case DialogueSelector dialogue:
					dialogue.Refill(
						parent: dialogue.DisplayContainer.Value,
						nodes: _dialogueSelectorDisplays,
						configs: Dialogues.AvailableDialogues,
						create: DialogueSelector.DialogueDisplay.Create
					);
					break;
				default:
					GD.PrintErr($"Unhandled refill for type {value.GetType().Name}");
					break;
			}
		}
	}
	public const string DefaultCommandPrefix = "\\";
	private static void InitConsole(Core core)
	{
		Console.Console.Command
		quitCommand = new() { Default = () => core.GetTree().Quit() },
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
		};
		ReadOnlySpan<(string, Console.Console.Command)> configs = [
			("quit", quitCommand),
			("dialogue", dialogueCommand),
		];
		Console.Console.Add(DefaultCommandPrefix, configs);

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

	public CoreUI Container
	{
		get
		{
			if (field is not null) return field;
			CoreUI ui = new CoreUI() { Name = "Core UI", Colours = Colours }
				.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.Minsize);
			AddChild(ui);
			ui.Menu.Signals = Handler;
			ui.Menu.Buttons.OnPressed = Handler;
			ui.Menu.Settings.Nonogram.SettingsChanger = Handler;
			Input.Bind(bindsContainer: ui.Menu.Settings.Input.InputsContainer,
				(Key.Escape, EscapePressed, "Toggle Main Menu"),
				(Key.Backslash, ToggleConsole, "Toggle Console")
			);
			return field = ui;
			static void ToggleConsole() => Console.Console.Container.Visible = !Console.Console.Container.Visible;
			void EscapePressed()
			{
				if (!ui.Menu.Visible)
				{
					ui.Menu.Show();
					ui.Menu.Buttons.Show();
					return;
				}
				ReadOnlySpan<Control> steps = [
					Console.Console.Container,
					Nonogram.UI.CompletionScreen,
					ui.Menu.Settings,
					ui.Menu.Levels,
					ui.Menu.Dialogues
				];
				foreach (Control control in steps)
				{
					if (control.Visible)
					{
						control.Hide();
						ui.Menu.Show();
						ui.Menu.Buttons.Show();
						return;
					}
				}
			}
		}
	}

	private EventHandler Handler => field ??= new(this);
	private NonogramEvents NonogramHandler => field ??= new(this);

	private CurrentPuzzle Nonogram => field ??= CurrentPuzzle
		.Create(Container)
		.ChangeEvents(NonogramHandler)
		.ChangeColour(Colours)
		.AddNonogramCommands();
	private Manager Minesweeper => field ??= Manager
		.Create(Container, Colours, Handler, Container.Menu)
		.AddMinesweeperCommands();

	public override void _Ready()
	{
		Name = nameof(Core);
		Dialogues.Instance.BuildDialogues();

		InitConsole(this);

		Nonogram.Type = PuzzleManager.Type.Game;
		Container.LoadingScreen.Show();

		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
	}
	public override void _Process(double delta)
	{
		Nonogram.Timer.Tick(delta);
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

