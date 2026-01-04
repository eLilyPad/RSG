using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;

public sealed partial class Core : Node, PuzzleManager.CurrentPuzzle.IPuzzleEvent
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

		Input.Bind(
			(Key.Escape, Container.StepBack, "Toggle Main Menu"),
			(Key.Backslash, CoreUI.ToggleConsole, "Toggle Console")
		);
		Container.Menu.Settings.Input.InputsContainer.RefreshBindings();

		CoreUI.ConnectSignals(Container);

		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);

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

	void PuzzleManager.CurrentPuzzle.IPuzzleEvent.Completed()
	{
		string dialogueName = PuzzleManager.Current.Puzzle.Expected.DialogueName;
		Dialogues.Enable(dialogueName);
	}

}

