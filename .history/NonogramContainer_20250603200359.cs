using Godot;

namespace RSG.UI;

public sealed partial class NonogramContainer : Container
{
	public static NonogramContainer Create<T>(
		T data,
		int length = 5,
		int scale = 40,
		int margin = 150
	) where T : IHavePenMode, IHaveColourPack, TilesContainer.IHandleButtonPress
	{
		Vector2I tilesSize = Vector2I.One * length;
		var background = new ColorRect
		{
			Name = "Background",
			Color = data.Colours.NonogramBackground,
			Size = tilesSize * (scale + 5)
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			margin
		);

		return new NonogramContainer
		{
			Name = nameof(NonogramContainer),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			Background = background,
			Tiles = TilesContainer.Create(data, length, scale, margin),
			Hints = HintsContainer.Hints(length),
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			margin
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
