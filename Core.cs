using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;

public sealed partial class Core : Node, PuzzleManager.IHaveEvents, MainMenu.IPress
{
	public const string
	ColourPackPath = "res://Data/DefaultColours.tres",
	DialoguesPath = "res://Data/Dialogues.tres";
	public static ColourPack Colours => field ??= ColourPackPath.LoadOrCreateResource<ColourPack>();
	public CoreUI Container => field ??= new CoreUI { Name = "Core UI", Colours = Colours }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	public override void _Ready()
	{
		Name = nameof(Core);
		NonogramContainer nonogram = PuzzleManager.Current.UI;

		this.Add(Container);
		Dialogues.Instance.BuildDialogues();

		CoreUI.ConnectSignals(Container);
		CoreUI.SetThemes(Container);
		Container.Menu.OnPressed = this;




		Input.Bind(bindsContainer: Container.Menu.Settings.Input.InputsContainer,
			(Key.Escape, Container.EscapePressed, "Toggle Main Menu"),
			(Key.Backslash, CoreUI.ToggleConsole, "Toggle Console")
		);
		Console.Add("/", ("quit", new Console.Command { Default = () => GetTree().Quit() }),
			("nonogram", new Console.Command
			{
				Default = () => Console.Log("Current Display: " + PuzzleManager.Current.Type.AsName()),
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
		PuzzleManager.Current.EventHandler = this;
		PuzzleManager.Current.Settings = new();
		static void ChangeDisplayType(Display.Type type)
		{
			PuzzleManager.Current.Type = type;
			Console.Log($"Display changed too {type.AsName()}");
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
			Dialogues.Next();
			return;
		}
		Input.RunEvent(input);
	}

	public void Completed(SaveData puzzle)
	{
		string dialogueName = puzzle.Expected.DialogueName;
		PuzzleManager.Current.UI.CompletionScreen.Show();
		Dialogues.Enable(dialogueName);
	}
	public void SettingsChanged()
	{
		SettingsMenuContainer menu = Container.Menu.Settings.Nonogram;
		Settings settings = PuzzleManager.Current.Settings;

		menu.AutoCompletion.LockFilledTiles.Value.ButtonPressed = settings.LockCompletedFilledTiles;
		menu.AutoCompletion.LockBlockedTiles.Value.ButtonPressed = settings.LockCompletedBlockedTiles;
		menu.AutoCompletion.BlockCompleteLines.Value.ButtonPressed = settings.LineCompleteBlockRest;

	}
	public void PlayPressed()
	{
		PuzzleManager.CurrentPuzzle current = PuzzleManager.Current;
		switch (current)
		{
			case { PuzzleReady: true }:
				Container.Menu.Hide();
				PuzzleManager.Current.UI.Show();
				break;
			case { PuzzleReady: false }:
				Container.Menu.Levels.Show();
				Container.Menu.Show();
				break;
			default:
				break;
		}
		Container.Menu.Buttons.Hide();
	}
	public void LevelsPressed()
	{
		Container.Menu.Levels.Show();
		Container.Menu.Buttons.Hide();
	}
	public void DialoguesPressed()
	{
		Container.Menu.Dialogues.Show();
		Container.Menu.Buttons.Hide();
	}
	public void SettingsPressed()
	{
		Container.Menu.Settings.Show();
		Container.Menu.Buttons.Hide();
	}
	public void QuitPressed()
	{
		GetTree().Quit();
	}
}

