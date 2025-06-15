using Godot;

namespace RSG.UI;

public abstract partial class NonogramDisplay : Container
{
	public const string BlockText = "X", FillText = "O", EmptyText = " ";

	public Dictionary<Vector2I, Button> Buttons { get; } = [];
	public Dictionary<Vector2I, RichTextLabel> Hints { get; } = [];

	public GridContainer Tiles { get; init => field = InitTiles(value); } = new();
	public abstract ColorRect Background { get; }
	public virtual (BoxContainer Rows, BoxContainer Columns) HintContainers { get; } = (
		new BoxContainer
		{
			Name = "RowHints",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		),
		new BoxContainer
		{
			Name = "ColumnHints",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		)
	);
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
		(SizeFlags, SizeFlags) expandFill = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);

		Name = nameof(NonogramDisplay);
		(SizeFlagsHorizontal, SizeFlagsVertical) = expandFill;

		Tiles.Name = "Tiles";
		(Tiles.SizeFlagsHorizontal, Tiles.SizeFlagsVertical) = expandFill;
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

	protected virtual T InitTiles<T>(T value) where T : GridContainer
	{
		value.Name = "Tiles";
		(value.SizeFlagsHorizontal, value.SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		value.SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);
		return value;
	}
}
