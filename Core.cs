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
	PuzzleManager.IHaveEvents,
	IHandleEvents,
	MainMenu.IPress,
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
			switch (current)
			{
				case { PuzzleReady: true }:
					core.Container.Menu.Hide();
					PuzzleManager.Current.UI.Show();
					break;
				case { PuzzleReady: false }:
					core.Container.Menu.Levels.Show();
					core.Container.Menu.Show();
					break;
				default:
					break;
			}
			core.Container.Menu.Buttons.Hide();
		}
		public void LevelsPressed() => core.Container.Menu.Levels.Show();
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
			core.Container.Menu.Show();
		}
		public void Completed(Manager.Data data)
		{
			core.Container.Menu.Show();
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
			Default = () => Console.Console.Log("Current Display: " + PuzzleManager.Current.Type.AsName()),
			Flags = new()
			{
				["game"] = () => ChangeDisplayType(Display.Type.Game),
				["paint"] = () => ChangeDisplayType(Display.Type.Paint),
				["display"] = () => ChangeDisplayType(Display.Type.Display),
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
		static void ChangeDisplayType(Display.Type type)
		{
			PuzzleManager.Current.Type = type;
			Console.Console.Log($"Display changed too {type.AsName()}");
		}
	}

	public const string
	ColourPackPath = "res://Data/DefaultColours.tres",
	MinesweeperTexturesPath = "res://Data/MinesweeperTextures.tres",
	DialoguesPath = "res://Data/Dialogues.tres";
	public static ColourPack Colours => field ??= ColourPackPath.LoadOrCreateResource<ColourPack>();
	public CoreUI Container => field ??= new CoreUI { Name = "Core UI", Colours = Colours }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.Minsize);

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

			ui.Resized += () => ui.Background.Border.TextureBorder((Vector2I)ui.Size);

			return field = minesweeper;
		}
	}

	public override void _Ready()
	{
		Name = nameof(Core);

		this.Add(Container);
		Dialogues.Instance.BuildDialogues();

		CoreUI.ConnectSignals(Container);
		CoreUI.SetThemes(Container);
		Container.Menu.OverrideSignals(Handler);

		Input.Bind(bindsContainer: Container.Menu.Settings.Input.InputsContainer,
			(Key.Escape, Container.EscapePressed, "Toggle Main Menu"),
			(Key.Backslash, CoreUI.ToggleConsole, "Toggle Console")
		);
		InitConsole(this);

		PuzzleManager.Current.Type = Display.Type.Game;
		PuzzleManager.Current.EventHandler = Handler;

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

