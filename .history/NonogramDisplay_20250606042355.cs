using Godot;

namespace RSG.UI;

public abstract partial class NonogramDisplay : Container
{
	public const string BlockText = "X", FillText = "O", EmptyText = " ";

	public Dictionary<Vector2I, Button> Buttons { get; } = [];
	public Dictionary<Vector2I, RichTextLabel> Hints { get; } = [];

	public GridContainer Tiles { get; init => field = InitTiles(value); } = new();
	public ColorRect Background { get; init => field = InitBackGround(value); } = new();
	public (BoxContainer Rows, BoxContainer Columns) HintContainers
	{
		get; init
		{
			field = (InitHintContainer(value.Rows), InitHintContainer(value.Columns));
		}
	} = (new() { Name = "RowHints" }, new() { Name = "ColumnHints" });
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
		(SizeFlagsHorizontal, SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);

		this.Add(
			Background,
			Main.Add(Spacer, HintContainers.Rows, HintContainers.Columns, Tiles)
		);

		// Init Tile buttons
		Vector2I size = Vector2I.One * Tiles.Columns;
		foreach (Vector2I position in size.AsRange())
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

	protected virtual T InitBackGround<T>(T value) where T : ColorRect
	{
		value.Name = "Background";
		return value;
	}
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
	protected virtual T InitHintContainer<T>(T value) where T : BoxContainer
	{
		(value.SizeFlagsHorizontal, value.SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		value.SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);
		return value;
	}
}
