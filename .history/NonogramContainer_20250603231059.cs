using Godot;

namespace RSG.UI;

public sealed partial class NonogramContainer : Container
{
	public static NonogramContainer Displays(Nonogram nonogram, ColourPack colours)
	{
		return Create(tilePressed: nonogram, colours: colours, displaySettings: nonogram.Settings);
	}
	public static NonogramContainer PainterDisplay(
		TilesContainer.IHandleButtonPress tilePressed,
		ColourPack colours,
		Nonogram.DisplaySettings displaySettings
	)
	{
		return new NonogramContainer
		{
			Background = ColouredBackground(colours, displaySettings),
			Tiles = TilesContainer.Create(TilePressedHandler: tilePressed, displaySettings),
			Hints = HintsContainer.Hints(length: displaySettings.Length),
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: displaySettings.Margin
		);
	}

	public static NonogramContainer Create(
		TilesContainer.IHandleButtonPress tilePressed,
		ColourPack colours,
		Nonogram.DisplaySettings displaySettings
	)
	{
		return new NonogramContainer
		{
			Background = ColouredBackground(colours, displaySettings),
			Tiles = TilesContainer.Create(tilePressed, displaySettings),
			Hints = HintsContainer.Hints(displaySettings.Length),
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: displaySettings.Margin
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

	private NonogramContainer()
	{
		Name = nameof(NonogramContainer);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;
	}
	public override void _Ready()
	{
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
