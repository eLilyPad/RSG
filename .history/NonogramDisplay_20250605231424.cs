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

	public abstract Nonogram.DisplaySettings Settings { get; set; }

	public Dictionary<Vector2I, Button> Buttons { get; } = [];
	public Dictionary<Vector2I, RichTextLabel> Hints { get; } = [];

	public abstract (BoxContainer Rows, BoxContainer Columns) HintContainers { get; }
	public abstract ColorRect Background { get; }
	public abstract GridContainer Tiles { get; }
	public Control Spacer => field ??= new Control { Name = "Spacer", Size = Tiles.Size };
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
		(Tiles.SizeFlagsHorizontal, Tiles.SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		(HintContainers.Rows.SizeFlagsHorizontal, HintContainers.Rows.SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		(HintContainers.Columns.SizeFlagsHorizontal, HintContainers.Columns.SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		Tiles.SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		this.Add(
			Background,
			Main.Add(Spacer, HintContainers.Rows, HintContainers.Columns, Tiles)
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
