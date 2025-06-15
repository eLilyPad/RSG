using Godot;

namespace RSG.UI;

public sealed partial class Nonogram<T>(T data) : Container where T : IHavePenMode, IHaveColourPack
{
	public Container Tiles => field ??= new NonogramTilesContainer<T>(data);

	public override void _Ready()
	{
		Name = nameof(Nonogram<>);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		this.Add(Tiles);
	}

}
public sealed partial class NonogramTilesContainer<T> : GridContainer where T : IHavePenMode, IHaveColourPack
{
	private const string BlockText = "X", FillText = "O", EmptyText = " ";
	private const int GridScale = 40, GridMargin = 150, GridLength = 5;
	private static Vector2I GridSize => Vector2I.One * GridLength;

	public GridContainer Grid => field ??= new GridContainer()
	{
		Name = "Grid",
		Columns = GridLength,
		Size = GridSize * GridScale
	}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.BottomRight,
		resizeMode: LayoutPresetMode.KeepSize,
		margin: GridMargin
	);

	public ColorRect Background => field ??= new ColorRect
	{
		Name = "Background",
		Size = GridSize * (GridScale + 5)
	}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.BottomRight,
		resizeMode: LayoutPresetMode.KeepSize,
		margin: GridMargin
	);

	public HBoxContainer ColumnHints => field ??= new HBoxContainer
	{
		Name = "Column Hints",
		SizeFlagsHorizontal = SizeFlags.ExpandFill,
		SizeFlagsVertical = SizeFlags.ShrinkCenter
	}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.TopLeft,
		resizeMode: LayoutPresetMode.KeepSize,
		margin: GridMargin
	);

	public VBoxContainer RowHints => field ??= new VBoxContainer
	{
		Name = "Column Hints",
		SizeFlagsHorizontal = SizeFlags.ExpandFill,
		SizeFlagsVertical = SizeFlags.ShrinkCenter
	}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.TopLeft,
		resizeMode: LayoutPresetMode.KeepSize,
		margin: GridMargin
	);

	public Dictionary<Vector2I, Button> Buttons { get; } = [];

	public NonogramTilesContainer(T data)
	{
		Assert(Grid is not null || ColumnHints is not null || RowHints is not null);

		Columns = 2;
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		var spacer = new Control
		{
			Name = "Spacer",
			Size = GridSize * GridScale
		};

		this.Add(spacer, ColumnHints, RowHints, Grid);

		foreach (Vector2I position in GridSize.AsRange())
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
			Grid.AddChild(button);

			button.Pressed += () => button.Text = data.CurrentPenMode switch
			{
				Core.PenMode.Block => button.Text switch
				{
					EmptyText => BlockText,
					BlockText => FillText,
					_ => EmptyText
				},
				Core.PenMode.Fill => button.Text switch
				{
					EmptyText => BlockText,
					BlockText => FillText,
					_ => EmptyText
				},
				_ => button.Text
			};
		}

	}
}
