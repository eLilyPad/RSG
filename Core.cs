using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;

public interface IHaveCore { Core Core { get; init; } }

public sealed partial class Core : Node
{
	public const string ColourPackPath = "res://Data/DefaultColours.tres";

	public AspectRatioContainer Container { get; } = new AspectRatioContainer
	{
		Name = "Container",
		Ratio = 16f / 9f,
		StretchMode = AspectRatioContainer.StretchModeEnum.Fit,
	}
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public TitleScreenContainer LoadingScreen { get; } = new TitleScreenContainer { Name = "Loading Screen", }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public NonogramContainer Nonogram { get; } = new NonogramContainer { Name = "Nonogram" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public PuzzleSelectorContainer Levels { get; } = new() { Name = "Level Selector", Visible = false };

	public MainMenu Menu => field ??= new() { Colours = Colours };
	public ColourPack Colours => field ??= ColourPackPath.LoadOrCreateResource<ColourPack>();

	public override void _Ready()
	{
		Name = nameof(Core);
		this.Add(Container.Add(Nonogram, Menu, Levels, LoadingScreen));

		Input.Bind((Key.Escape, StepBack, "Toggle Main Menu"));
		Menu.Settings.Input.InputsContainer.RefreshBindings();

		Menu.Init(Levels, Colours);
		Nonogram.Init();
		Levels.Init();

		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);

		void StepBack() => Menu.StepBack(Menu.Settings, Levels);
	}
	public override void _Input(InputEvent input)
	{
		if (input.IsPressed() && LoadingScreen.Visible)
		{
			LoadingScreen.Hide();
			return;
		}
		Input.RunEvent(input);
	}
	public sealed partial class TitleScreenContainer : PanelContainer
	{
		public ColorRect Background { get; } = new ColorRect
		{
			Name = "Background",
			Color = Colors.Aquamarine,
		}.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public RichTextLabel LoadingText { get; } = new RichTextLabel
		{
			Name = "Loading Text",
			BbcodeEnabled = true,
			Text = "[color=black][font_size=60] Press Anything To Continue...",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			FitContent = true,
		}.Preset(preset: LayoutPreset.Center, resizeMode: LayoutPresetMode.KeepSize);
		public override void _Ready()
		{
			this.Add(Background, LoadingText);
		}
	}
}

