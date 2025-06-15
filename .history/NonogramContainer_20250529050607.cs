using Godot;

namespace RSG.UI;

public sealed partial class Nonogram<T> : Container where T : IHavePenMode, IHaveColourPack
{
	public required T Data { get; init; }
	public Container Tiles => field ??= new NonogramTilesContainer<T>(Data);

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
public sealed partial class NonogramTilesContainer<T> : Container where T : IHavePenMode, IHaveColourPack
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
		preset: LayoutPreset.CenterRight,
		resizeMode: LayoutPresetMode.KeepSize,
		margin: GridMargin
	);

	public ColorRect Background => field ??= new ColorRect
	{
		Name = "Background",
		Size = GridSize * (GridScale + 5)
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.CenterRight,
		resizeMode: LayoutPresetMode.KeepSize,
		margin: GridMargin
	);

	public Dictionary<Vector2I, Button> Buttons { get; } = [];

	public NonogramTilesContainer(T data)
	{
		AddChild(Grid);

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
			Grid?.AddChild(button);

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
