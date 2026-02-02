using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;
using Minesweeper;
using Dialogue;

public sealed partial class Core : Node
{
	private sealed class EventHandler(Core core) :
	IManagePuzzle,
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
			NonogramContainer nonogram = PuzzleManager.Current.UI;
			MinesweeperContainer minesweeper = core.Minesweeper.UI;
			if (!core.Container.Menu.Visible) { return; }
			if (nonogram.Visible) { nonogram.Hide(); }
			if (minesweeper.Visible) { minesweeper.Hide(); }
		}
		public void Completed(SaveData puzzle)
		{
			string dialogueName = puzzle.Expected.DialogueName;
			PuzzleManager.Current.UI.CompletionScreen.Show();
			Dialogues.Enable(dialogueName);
		}
		public void SettingsChanged()
		{
			SettingsMenuContainer menu = core.Container.Menu.Settings.Nonogram;
			Settings settings = PuzzleManager.Current.Settings;

			menu.AutoCompletion.LockFilledTiles.Value.ButtonPressed = settings.LockCompletedFilledTiles;
			menu.AutoCompletion.LockBlockedTiles.Value.ButtonPressed = settings.LockCompletedBlockedTiles;
			menu.AutoCompletion.BlockCompleteLines.Value.ButtonPressed = settings.LineCompleteBlockRest;
		}
		public void PlayMinesweeperPressed()
		{
			core.Minesweeper.Puzzle = Manager.Data.CreateRandom(10);
			core.Minesweeper.UI.Show();
			core.Container.Menu.Hide();
		}
		public void PlayPressed()
		{
			PuzzleManager.CurrentPuzzle current = PuzzleManager.Current;
			if (current.PuzzleReady)
			{
				core.Container.Menu.Hide();
				current.UI.Show();
				current.Type = Display.Type.Game;
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
			PuzzleManager.CurrentPuzzle current = PuzzleManager.Current;

			core.Container.Menu.Hide();
			core.Container.Menu.Buttons.Hide();

			current.UI.Show();
			current.Type = Display.Type.Paint;
			current.Puzzle = new(new(10));
		}
		public void LevelsPressed()
		{
			PuzzleManager.CurrentPuzzle current = PuzzleManager.Current;
			core.Container.Menu.Levels.Show();
			current.Type = Display.Type.Game;
		}
		public void DialoguesPressed() => core.Container.Menu.Dialogues.Show();
		public void SettingsPressed() => core.Container.Menu.Settings.Show();
		public void QuitPressed() => core.GetTree().Quit();
		public void ToggledLockFilledTiles(bool toggled)
		{
			PuzzleManager.CurrentPuzzle current = PuzzleManager.Current;
			current.Settings = current.Settings with { LockCompletedFilledTiles = toggled };
		}
		public void ToggledLockBlockedTiles(bool toggled)
		{
			PuzzleManager.CurrentPuzzle current = PuzzleManager.Current;
			current.Settings = current.Settings with { LockCompletedBlockedTiles = toggled };
		}
		public void ToggledBlockCompleteLines(bool toggled)
		{
			PuzzleManager.CurrentPuzzle current = PuzzleManager.Current;
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
					puzzle.Refill(
						parent: puzzle.Puzzles.Value,
						nodes: _levelSelectorDisplays,
						configs: PuzzleManager.SelectorConfigs,
						create: PuzzleSelector.PackDisplay.Create
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
		},
		nonogramCommand = new()
		{
			Default = () => Console.Console.Log("do nothing, show help"),
			Flags = new()
			{

			}
		};
		ReadOnlySpan<(string, Console.Console.Command)> configs = [
			("quit", quitCommand),
			("dialogue", dialogueCommand),
			("nonogram", nonogramCommand)
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
			return field = ui;
		}
	}

	private EventHandler Handler => field ??= new(this);



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
			Manager minesweeper = new() { UI = ui, EventHandler = Handler };

			Container.AddChild(ui);
			ui.Tiles.Provider = minesweeper;

			ui.CompletionScreen.Value.Options.MainMenu.Pressed += () =>
			{
				ui.CompletionScreen.Hide();
				ui.Hide();
				Container.Menu.Show();
			};
			Console.Console.Command command = new()
			{
				Flags = new()
				{
					["new"] = () =>
					{
						minesweeper.Puzzle = Manager.Data.CreateRandom(10);
						minesweeper.UI.Show();
						Console.Console.Log("Started new Minesweeper game");
					},
					["uncover_all"] = () =>
					{
						minesweeper.UI.Tiles.ShowAll();
						minesweeper.UI.Show();
						Console.Console.Log("Uncovering all tiles");
					}
				}
			};
			Console.Console.Add(DefaultCommandPrefix, ("minesweeper", command));

			return field = minesweeper;
		}
	}

	public override void _Ready()
	{
		Name = nameof(Core);
		Dialogues.Instance.BuildDialogues();

		InitConsole(this);

		PuzzleManager.Current.Type = Display.Type.Game;
		PuzzleManager.Current.EventHandler = Handler;

		Container.LoadingScreen.Show();

		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
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

