using Godot;

namespace RSG.UI;

public sealed partial class MainMenu : Container
{
	public required ColourPack Colours { get; init; }

	public override void _Ready()
	{
		Name = nameof(MainMenu);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		var background = new ColorRect
		{
			Name = "Margin Background",
			Color = Colours.MainMenuBackground,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			100
		);

		this.Add(background);
	}

	public sealed partial class Buttons : VBoxContainer
	{
		public Buttons()
		{
			Name = nameof(Buttons);
			SizeFlagsHorizontal = SizeFlags.ExpandFill;
			SizeFlagsVertical = SizeFlags.ExpandFill;

			SetAnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);
		}
	}
}