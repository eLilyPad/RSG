using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;

public sealed partial class CoreContainer : AspectRatioContainer
{
	public TitleScreenContainer LoadingScreen { get; } = new TitleScreenContainer { Name = "Loading Screen", }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public NonogramContainer Nonogram { get; } = new NonogramContainer { Name = "Nonogram" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public PuzzleSelector Levels { get; } = new PuzzleSelector { Name = "Level Selector", Visible = false };
	public MainMenu Menu { get; } = new MainMenu { Name = "MainMenu", Colours = Core.Colours };
	public override void _Ready() => this.Add(Nonogram, Menu, Dialogues.Container, LoadingScreen);
}
public sealed partial class Core : Node
{
	public const string
	ColourPackPath = "res://Data/DefaultColours.tres",
	DialoguesPath = "res://Data/Dialogues.tres";
	public static ColourPack Colours => field ??= ColourPackPath.LoadOrCreateResource<ColourPack>();
	public CoreContainer Container => field ??= new CoreContainer
	{
		Name = "Core UI",
		Ratio = 16f / 9f,
		StretchMode = AspectRatioContainer.StretchModeEnum.Fit,
	}.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize, 40);

	public override void _Ready()
	{
		Name = nameof(Core);
		this.Add(Container);
		Dialogues.Instance.BuildDialogues();

		Input.Bind((Key.Escape, StepBack, "Toggle Main Menu"));
		Container.Menu.Settings.Input.InputsContainer.RefreshBindings();

		Container.Menu.Init(Colours);
		Container.Nonogram.Init(Container.Menu);

		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);

		void StepBack() => Container.Menu.StepBack(Container.Menu.Settings, Container.Menu.Levels, Container.Menu.Dialogues);
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

