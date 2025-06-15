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
			margin: displaySettings.Margin
		);
	}

	public const string BlockText = "X", FillText = "O", EmptyText = " ";

	public abstract TTiles Tiles { get; }
	public abstract (THints Rows, THints Columns) Hints { get; }
	public abstract ColorRect Background { get; }
	public Control Spacer => field ??= new Control { Name = "Spacer", Size = Tiles.Size };
	public GridContainer GridTiles => field ??= new GridContainer
	{
		Name = "Tiles",
		SizeFlagsHorizontal = SizeFlags.ExpandFill,
		SizeFlagsVertical = SizeFlags.ExpandFill
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.FullRect,
		resizeMode: LayoutPresetMode.KeepSize
	);
	public GridContainer MainContainer => field ??= new GridContainer
	{
		Name = "MainContainer",
		Columns = 2,
		Size = Tiles.Size
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.FullRect,
		resizeMode: LayoutPresetMode.KeepSize
	);

	public Dictionary<Vector2I, Button> Buttons { get; } = [];

	public override void _Ready()
	{
		Name = nameof(NonogramDisplay<,>);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		this.Add(
			Background,
			MainContainer.Add(Spacer, Hints.Rows, Hints.Columns, Tiles)
		);

		foreach (Vector2I position in (Vector2I.One * GridTiles.Columns).AsRange())
		{
			var button = Buttons[position] = new Button
			{
				Name = $"Button {position}",
				Text = EmptyText,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			}.AnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);
			AddChild(button);

			button.Pressed += () => OnTilePressed(position, button);
		}
	}
	public abstract void OnTilePressed(Vector2I position, Button button);
}
