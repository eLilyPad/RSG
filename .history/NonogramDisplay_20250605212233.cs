using Godot;

namespace RSG.UI;

public abstract partial class NonogramDisplay<TTiles, THints> : Container
where TTiles : GridContainer
where THints : BoxContainer
{
	public static ColorRect ColouredBackground(ColourPack colours, Nonogram.DisplaySettings displaySettings)
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

	public abstract TTiles Tiles { get; }
	public abstract (THints Rows, THints Columns) Hints { get; }
	public abstract ColorRect Background { get; }
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

	public override void _Ready()
	{
		Name = nameof(NonogramDisplay<,>);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;
		this.Add(
			Background,
			Grid.Add(Spacer, Hints.Rows, Hints.Columns, Tiles)
		);
	}
}
