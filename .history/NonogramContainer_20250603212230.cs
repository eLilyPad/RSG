using Godot;

namespace RSG.UI;

public sealed partial class NonogramContainer : Container
{
	public static NonogramContainer Create<T>(
		T data,
		ColourPack colours,
		in (int length, int scale, int margin) displaySettings
	) where T : IHavePenMode, TilesContainer.IHandleButtonPress
	{
		Vector2I tilesSize = Vector2I.One * displaySettings.length;
		var background = new ColorRect
		{
			Name = "Background",
			Color = colours.NonogramBackground,
			Size = tilesSize * (displaySettings.scale + 5)
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			displaySettings.margin
		);

		return new NonogramContainer
		{
			Name = nameof(NonogramContainer),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			Background = background,
			Tiles = TilesContainer.Create(data, colours, displaySettings),
			Hints = HintsContainer.Hints(displaySettings.length),
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			displaySettings.margin
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
}
