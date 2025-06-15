using Godot;

namespace RSG.UI;

public abstract partial class NonogramDisplay : Container
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

	public Dictionary<Vector2I, Button> Buttons { get; } = [];

	public abstract Nonogram.DisplaySettings Settings { get; set; }

	public abstract (BoxContainer Rows, BoxContainer Columns) Hints { get; }
	public abstract ColorRect Background { get; }
	public Control Spacer => field ??= new Control { Name = "Spacer", Size = Tiles.Size };
	public abstract GridContainer Tiles { get; }
	// => field ??= new GridContainer
	//{
	//	Columns = Settings.Length,
	//	Size = Settings.TileSize
	//}.AnchorsAndOffsetsPreset(
	//	preset: LayoutPreset.FullRect,
	//	resizeMode: LayoutPresetMode.KeepSize
	//);
	public GridContainer Main => field ??= new GridContainer
	{
		Name = "MainContainer",
		Columns = 2,
		Size = Tiles.Size
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.FullRect,
		resizeMode: LayoutPresetMode.KeepSize
	);

	public override void _Ready()
	{
		Name = nameof(NonogramDisplay);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		Tiles.Name = "Tiles";
		Tiles.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		Tiles.SizeFlagsVertical = SizeFlags.ExpandFill;
		Tiles.SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		this.Add(
			Background,
			Main.Add(Spacer, Hints.Rows, Hints.Columns, Tiles)
		);

		foreach (Vector2I position in (Vector2I.One * Tiles.Columns).AsRange())
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
