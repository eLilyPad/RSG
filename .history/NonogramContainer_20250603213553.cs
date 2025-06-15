using Godot;

namespace RSG.UI;

public sealed partial class NonogramContainer : Container
{
	public static NonogramContainer PainterDisplay<T>(
		T data,
		ColourPack colours,
		Nonogram.DisplaySettings displaySettings
	) where T : TilesContainer.IHandleButtonPress
	{
		Vector2I tilesSize = Vector2I.One * displaySettings.Length;
		var background = new ColorRect
		{
			Name = "Background",
			Color = colours.NonogramBackground,
			Size = displaySettings.Size
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			displaySettings.Margin
		);

		return new NonogramContainer
		{
			Name = nameof(NonogramContainer),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			Background = ColouredBackground(colours, displaySettings),
			Tiles = TilesContainer.Create(data, colours, displaySettings),
			Hints = HintsContainer.Hints(displaySettings.Length),
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			displaySettings.Margin
		);
	}

	public static NonogramContainer Create<T>(
		T data,
		ColourPack colours,
		Nonogram.DisplaySettings displaySettings
	) where T : IHavePenMode, TilesContainer.IHandleButtonPress
	{
		var background = new ColorRect
		{
			Name = "Background",
			Color = colours.NonogramBackground,
			Size = displaySettings.BackgroundSize
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			displaySettings.Margin
		);

		return new NonogramContainer
		{
			Name = nameof(NonogramContainer),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			Background = background,
			Tiles = TilesContainer.Create(data, colours, displaySettings),
			Hints = HintsContainer.Hints(displaySettings.Length),
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			displaySettings.Margin
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
		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

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
