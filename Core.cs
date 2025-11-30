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
	public LoadingScreenContainer LoadingScreen { get; } = new LoadingScreenContainer { Name = "Loading Screen", }
	.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public NonogramContainer Nonogram { get; } = new() { Name = "Nonogram" };

	public MainMenu Menu => field ??= new() { Colours = Colours };
	public ColourPack Colours => field ??= ColourPackPath.LoadOrCreateResource<ColourPack>();

	public override void _Ready()
	{
		Name = nameof(Core);
		this.Add(Container.Add(Nonogram, Menu, LoadingScreen));

		Input.Bind((Key.Escape, Menu.Step, "Toggle Main Menu"));

		Menu.Settings.Input.InputsContainer.RefreshBindings();
		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
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
	public override void _Process(double delta)
	{
		//Vector2I scale = Vector2I.One * (GetTree().Root.Size.LengthSquared() / 4_000);
		//Nonogram.Displays.CurrentTabDisplay.Scale = Vector2I.One * (scale / 4);
		//GD.Print("Scale: ", scale);
	}

	public sealed partial class LoadingScreenContainer : PanelContainer
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

