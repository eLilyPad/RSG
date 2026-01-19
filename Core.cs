using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;


public sealed partial class Core : Node, PuzzleManager.IHaveEvents, MainMenu.IPress
{
	private sealed class CoreEventHandler(Core core) : PuzzleManager.IHaveEvents, MainMenu.IPress
	{
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
			core.Minesweeper.Puzzle = RSG.Minesweeper.Minesweeper.Data.CreateRandom(10);
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
		public void LevelsPressed()
		{
			core.Container.Menu.Levels.Show();
			core.Container.Menu.Buttons.Hide();
		}
		public void DialoguesPressed()
		{
			core.Container.Menu.Dialogues.Show();
			core.Container.Menu.Buttons.Hide();
		}
		public void SettingsPressed()
		{
			core.Container.Menu.Settings.Show();
			core.Container.Menu.Buttons.Hide();
		}
		public void QuitPressed()
		{
			core.GetTree().Quit();
		}
	}
	public const string
	ColourPackPath = "res://Data/DefaultColours.tres",
	MinesweeperTexturesPath = "res://Data/MinesweeperTextures.tres",
	DialoguesPath = "res://Data/Dialogues.tres";
	public static ColourPack Colours => field ??= ColourPackPath.LoadOrCreateResource<ColourPack>();
	public CoreUI Container => field ??= new CoreUI { Name = "Core UI", Colours = Colours }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.Minsize);

	private CoreEventHandler Events => field ??= new(this);
	private Minesweeper.Minesweeper Minesweeper
	{
		get
		{
			if (field is not null) return field;
			Minesweeper.MinesweeperContainer ui = new Minesweeper.MinesweeperContainer(Colours)
			{
				Name = "Minesweeper",
				Visible = false
			}.Preset(LayoutPreset.FullRect);
			Minesweeper.Minesweeper minesweeper = new() { UI = ui };

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
		Container.Menu.OnPressed = Events;

		Input.Bind(bindsContainer: Container.Menu.Settings.Input.InputsContainer,
			(Key.Escape, Container.EscapePressed, "Toggle Main Menu"),
			(Key.Backslash, CoreUI.ToggleConsole, "Toggle Console")
		);
		Console.Console.Add("/", ("quit", new Console.Console.Command { Default = () => GetTree().Quit() }),
			("nonogram", new Console.Console.Command
			{
				Default = () => Console.Console.Log("Current Display: " + PuzzleManager.Current.Type.AsName()),
				Flags = new()
				{
					["game"] = () => ChangeDisplayType(Display.Type.Game),
					["paint"] = () => ChangeDisplayType(Display.Type.Paint),
					["display"] = () => ChangeDisplayType(Display.Type.Display),
				}
			}
			)
		);

		PuzzleManager.Current.Type = Display.Type.Game;
		PuzzleManager.Current.EventHandler = Events;

		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);

		static void ChangeDisplayType(Display.Type type)
		{
			PuzzleManager.Current.Type = type;
			Console.Console.Log($"Display changed too {type.AsName()}");
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

