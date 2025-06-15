using Godot;

namespace RSG.UI;

public sealed partial class Nonogram<T>(T data) : Container where T : IHavePenMode, IHaveColourPack
{
	public Container Tiles => field ??= new TilesContainer<T>(data);
	public HBoxContainer ColumnHints => field ??= new HBoxContainer
	{
		Name = "Column Hints",
		SizeFlagsHorizontal = SizeFlags.ExpandFill,
		SizeFlagsVertical = SizeFlags.ShrinkCenter
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.TopLeft,
		resizeMode: LayoutPresetMode.KeepSize,
		margin: TilesContainer<T>.GridMargin
	);

	public VBoxContainer RowHints => field ??= new VBoxContainer
	{
		Name = "Column Hints",
		SizeFlagsHorizontal = SizeFlags.ExpandFill,
		SizeFlagsVertical = SizeFlags.ShrinkCenter
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.TopLeft,
		resizeMode: LayoutPresetMode.KeepSize,
		margin: TilesContainer<T>.GridMargin
	);

	public override void _Ready()
	{
		Name = nameof(Nonogram<>);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		var grid = new GridContainer()
		{
			Name = "Grid",
			Columns = TilesContainer<T>.GridLength,
			Size = TilesContainer<T>.GridSizeScaled
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.BottomRight,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: TilesContainer<T>.GridMargin
		);

		var spacer = new Control
		{
			Name = "Spacer",
			Size = TilesContainer<T>.GridSizeScaled
		};

		this.Add(grid.Add(spacer, ColumnHints, RowHints, Tiles));
	}

}
public sealed partial class TilesContainer<T> : GridContainer where T : IHavePenMode, IHaveColourPack
{
	public const string BlockText = "X", FillText = "O", EmptyText = " ";
	public const int GridScale = 40, GridMargin = 150, GridLength = 5;
	public static Vector2I GridSize => Vector2I.One * GridLength;
	public static Vector2I GridSizeScaled => Vector2I.One * GridLength * GridScale;

	public ColorRect Background => field ??= new ColorRect
	{
		Name = "Background",
		Size = GridSize * (GridScale + 5)
	}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.BottomRight,
		resizeMode: LayoutPresetMode.KeepSize,
		margin: GridMargin
	);

	public Dictionary<Vector2I, Button> Buttons { get; } = [];

	public TilesContainer(T data)
	{
		Name = "Tiles";
		Columns = GridLength;
		Size = GridSize * GridScale;
		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.BottomRight,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: GridMargin
		);

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
			AddChild(button);

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
