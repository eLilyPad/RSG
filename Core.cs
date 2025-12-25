using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;
using static Nonogram.NonogramContainer;

public sealed partial class Core : Node
{
	public const string
	ColourPackPath = "res://Data/DefaultColours.tres",
	DialoguesPath = "res://Data/Dialogues.tres";
	public static ColourPack Colours => field ??= ColourPackPath.LoadOrCreateResource<ColourPack>();
	public CoreUI Container => field ??= new CoreUI
	{
		Name = "Core UI",
		//Ratio = 16f / 9f,
		//StretchMode = AspectRatioContainer.StretchModeEnum.Fit,
	}.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	public override void _Ready()
	{
		Name = nameof(Core);
		Display.Data startPuzzle = PuzzleManager.Current.Puzzle;
		NonogramContainer nonogram = PuzzleManager.Current.UI;
		GameDisplay game = new() { Name = "Game", Status = nonogram.Status };
		PaintDisplay paint = new() { Name = "Paint" };
		Display.Default defaultDisplay = new() { Name = "Puzzle Display" };
		ReadOnlySpan<Display> displays = [game, paint, defaultDisplay];

		this.Add(Container);
		Dialogues.Instance.BuildDialogues();

		Input.Bind(
			(Key.Escape, Container.StepBack, "Toggle Main Menu"),
			(Key.Slash, CoreUI.ToggleConsole, "Toggle Console")
		);

		Container.Menu.Settings.Input.InputsContainer.RefreshBindings();

		Container.Menu.Background.Color = Colours.MainMenuBackground;
		nonogram.Background.Color = Colours.NonogramBackground;

		StyleBox baseBox = nonogram.Displays.GetThemeStylebox("normal");
		if (baseBox.Duplicate() is StyleBoxFlat stylebox)
		{
			stylebox.BgColor = Colors.Transparent;
			nonogram.Displays.AddThemeStyleboxOverride("panel", stylebox);
		}

		foreach (Display display in displays)
		{
			nonogram.Displays.Tabs.Add(display);
			nonogram.Displays.Add(display);
			int marginValue = 100;
			display.Margin.AddThemeConstantOverride("margin_top", marginValue);
			display.Margin.AddThemeConstantOverride("margin_bottom", marginValue / 2);
			display.Colours = Colours;
		}

		nonogram.ToolsBar.PuzzleLoader.Size = nonogram.ToolsBar.CodeLoader.Size = GetTree().Root.Size / 2;
		nonogram.ToolsBar.Saver.SetItems(clear: true, ("Save Puzzle", Key.None, SavePuzzlePressed));
		nonogram.ToolsBar.Loader.SetItems(
			false,
			("Load Puzzle", Key.None, () => nonogram.ToolsBar.PuzzleLoader.PopupCentered()),
			("Load From Code", Key.None, () => nonogram.ToolsBar.CodeLoader.PopupCentered())
		//("Load Current", Key.None, () => LoadCurrent(Displays.CurrentTabDisplay))
		);
		nonogram.Displays.CurrentTabDisplay.Load(PuzzleManager.Current.Puzzle);

		CoreUI.ConnectSignals(Container);

		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);

		Console.Add("/", ("quit", new Console.Command { Default = () => GetTree().Quit() }));

		void SavePuzzlePressed()
		{
			SaveData.Create(nonogram.Displays).Switch(
				save => PuzzleManager.Save(save),
				notFound => GD.Print("No current puzzle found")
			);
		}
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
}

