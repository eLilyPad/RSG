using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;
public sealed partial class Core : Node
{
	public const string
	ColourPackPath = "res://Data/DefaultColours.tres",
	DialoguesPath = "res://Data/Dialogues.tres";
	public static ColourPack Colours => field ??= ColourPackPath.LoadOrCreateResource<ColourPack>();
	public CoreUI Container => field ??= new CoreUI
	{
		Name = "Core UI",
		Ratio = 16f / 9f,
		StretchMode = AspectRatioContainer.StretchModeEnum.Fit,
	}.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	public override void _Ready()
	{
		Name = nameof(Core);
		Display.Data startPuzzle = PuzzleManager.Current.Puzzle;

		NonogramContainer.GameDisplay game = new()
		{
			Name = "Game",
			Status = Container.Nonogram.Status
		};
		NonogramContainer.PaintDisplay paint = new() { Name = "Paint", };
		ReadOnlySpan<Display> displays = [game, paint, Display.Default];

		this.Add(Container);
		Dialogues.Instance.BuildDialogues();

		Input.Bind((Key.Escape, Container.StepBack, "Toggle Main Menu"));

		Container.Menu.Settings.Input.InputsContainer.RefreshBindings();
		Container.Colours = Colours;

		foreach (Display display in displays)
		{
			Container.Nonogram.Displays.Tabs.Add(display);
			Container.Nonogram.Displays.Add(display);
		}

		Container.Nonogram.ToolsBar.PuzzleLoader.Size = Container.Nonogram.ToolsBar.CodeLoader.Size = GetTree().Root.Size / 2;
		Container.Nonogram.ToolsBar.Saver.SetItems(clear: true, ("Save Puzzle", Key.None, SavePuzzlePressed));
		Container.Nonogram.ToolsBar.Loader.SetItems(
			false,
			("Load Puzzle", Key.None, () => Container.Nonogram.ToolsBar.PuzzleLoader.PopupCentered()),
			("Load From Code", Key.None, () => Container.Nonogram.ToolsBar.CodeLoader.PopupCentered())
		//("Load Current", Key.None, () => LoadCurrent(Displays.CurrentTabDisplay))
		);

		Container.Nonogram.Displays.CurrentTabDisplay.Load(PuzzleManager.Current.Puzzle);

		Vector2I guideSize = (Vector2I)game.TilesGrid.Size / 2;
		guideSize = guideSize with { Y = guideSize.X };
		game.Guides.CreateLines(size: guideSize);
		paint.Guides.CreateLines(size: guideSize);
		Display.Default.Guides.CreateLines(size: guideSize);

		CoreUI.ConnectSignals(Container);

		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);

		void SavePuzzlePressed()
		{
			SaveData.Create(Container.Nonogram.Displays).Switch(
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

