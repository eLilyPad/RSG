using Godot;

namespace RSG.UI;

public partial class NonogramPainterContainer : Container
{
	public static NonogramPainterContainer Create<T>(
		T data,
		ColourPack colours,
		Nonogram.DisplaySettings displaySettings
	)
	where T : IHavePenMode, TilesContainer.IHandleButtonPress
	{
		return new NonogramPainterContainer
		{
			Name = nameof(NonogramPainterContainer),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			Tiles = TilesContainer.Create(data, colours, displaySettings),
			Hints = HintsContainer.Hints(displaySettings.Length)
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			displaySettings.Margin
		);
	}

	public required TilesContainer Tiles { get; init; }
	public required (HintsContainer Rows, HintsContainer Columns) Hints { get; init; }

	private NonogramPainterContainer() { }
}
