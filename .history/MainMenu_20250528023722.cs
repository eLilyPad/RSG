using Godot;

namespace RSG;

public sealed partial class MainMenu : Container
{
	public required ColourPack Colours { get; init; }
	public sealed partial class Buttons : VBoxContainer
	{
		public Buttons()
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill;
			SizeFlagsVertical = SizeFlags.ExpandFill;
			SetAnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);
		}
	}
	public override void _Ready()
	{
		Name = "Main Menu";

		var margin = new MarginContainer
		{
			Name = "Margin",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: 30
		);

		var marginBackground = new ColorRect
		{
			Name = "Margin Background",
			Color = Colours.MainMenuBackground,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		this.Add(
			margin.Add(marginBackground)
		);
	}
}