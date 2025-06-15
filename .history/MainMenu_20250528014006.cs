using Godot;

namespace RSG;

public sealed class MainMenu : Container
{
	public sealed class Buttons : VBoxContainer
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
		}.SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: 30
		);

		var marginBackground = new ColorRect
		{
			Name = "Margin Background",
			Color = new Color(0.1f, 0.1f, 0.1f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		}.SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		this.Add(
			margin.Add(marginBackground)
		);
	}
}