using Godot;

namespace RSG.UI;

public partial class NonogramPainterContainer : Container
{
	public static NonogramPainterContainer Create<T>(
		T data,
		(int length, int scale, int margin) displaySettings
	)
	where T : IHavePenMode, IHaveColourPack, TilesContainer.IHandleButtonPress
	{
		return new NonogramPainterContainer
		{
			Name = nameof(NonogramPainterContainer),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			Tiles = TilesContainer.Create(data, displaySettings),
			Hints = HintsContainer.Hints(displaySettings.length)
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			displaySettings.margin
		);
	}

	public required TilesContainer Tiles { get; init; }
	public required (HintsContainer Rows, HintsContainer Columns) Hints { get; init; }

	private NonogramPainterContainer() { }
}
