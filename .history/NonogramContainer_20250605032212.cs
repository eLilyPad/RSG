using Godot;

namespace RSG.UI;

public sealed partial class NonogramContainer : Container
{
	public static NonogramContainer Game(Nonogram nonogram)
	{
		return new NonogramContainer
		{
			Background = ColouredBackground(nonogram.Colours, nonogram.Settings),
			Tiles = TilesContainer.Create(nonogram, nonogram.Settings),
			Hints = HintsContainer.Hints(nonogram.Settings.Length),
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: nonogram.Settings.Margin
		);
	}
	public static NonogramContainer Painting(Nonogram nonogram)
	{
		return new NonogramContainer
		{
			Background = ColouredBackground(nonogram.Colours, nonogram.Settings),
			Tiles = TilesContainer.Create(nonogram, nonogram.Settings),
			Hints = HintsContainer.Hints(nonogram.Settings.Length),
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: nonogram.Settings.Margin
		);
	}

	public required TilesContainer Tiles { get; init; }
	public required (HintsContainer Rows, HintsContainer Columns) Hints { get; init; }
	public required ColorRect Background { get; init; }
	public Control Spacer => field ??= new Control { Name = "Spacer", Size = Tiles.Size };
	public GridContainer Grid => field ??= new GridContainer
	{
		Name = "Grid",
		Columns = 2,
		Size = Tiles.Size
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.FullRect,
		resizeMode: LayoutPresetMode.KeepSize
	);

	private NonogramContainer() { }
	public override void _Ready()
	{
		Name = nameof(NonogramContainer);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;
		this.Add(
			Background,
			Grid.Add(Spacer, Hints.Rows, Hints.Columns, Tiles)
		);
	}

	private static ColorRect ColouredBackground(
		ColourPack colours,
		Nonogram.DisplaySettings displaySettings
	)
	{
		return new ColorRect
		{
			Name = "Background",
			Color = colours.NonogramBackground,
			Size = displaySettings.BackgroundSize
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			displaySettings.Margin
		);
	}
}
