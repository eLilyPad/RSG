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
	private PuzzleSelector.LevelDisplays LevelDisplays => field ??= new(Core: this);
	private PuzzleSelector.StudioDisplays StudioDisplays => field ??= new(Core: this);
	private readonly List<PuzzleSelector.PuzzleDisplay> _studioPuzzleSelectorDisplays = [];
	private readonly List<DialogueSelector.DialogueDisplay> _dialogueSelectorDisplays = [];
	private Manager Minesweeper => field ??= Manager.Create(Container, _handler, Colours);
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

		void DialogueFinished()
		{
			Container.Menu.Show();
			Container.Menu.Buttons.Show();
		}
	}


}