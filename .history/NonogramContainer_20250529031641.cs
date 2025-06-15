using Godot;

namespace RSG.UI;

public sealed partial class NonogramContainer<T> : Container where T : IHavePenMode, IHaveColourPack
{
	private const int GridSize = 5;
	private const string BlockText = "X", FillText = "O", EmptyText = " ";

	public required T Data { get; init; }
	public GridContainer Grid { get; } = new GridContainer
	{
		Name = "Grid",
		Columns = GridSize,
		Size = Vector2I.One * GridSize * 40
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.CenterRight,
		resizeMode: LayoutPresetMode.KeepSize,
		margin: 150
	);

	private readonly Dictionary<Vector2I, Button> _buttons = [];

	public override void _Ready()
	{
		Name = nameof(NonogramContainer<>);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);
		var background = new ColorRect
		{
			Name = "Margin Background",
			Color = Data.Colours.NonogramBackground,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		this.Add(background, Grid);

		for (int x = 0; x < GridSize; x++)
		{
			for (int y = 0; y < GridSize; y++)
			{
				Grid.Add(_buttons[new(x, y)] = new NonogramButton(x, y) { Data = Data });
			}
		}
	}

	private sealed partial class NonogramButton : Button
	{
		public required T Data { get; init; }
		public NonogramButton(int x, int y)
		{
			Name = $"Button {x}, {y}";
			Text = EmptyText;
			SizeFlagsHorizontal = SizeFlags.ExpandFill;
			SizeFlagsVertical = SizeFlags.ExpandFill;

			SetAnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);
		}

		public override void _Ready()
		{
			Pressed += () =>
			{
				Text = Data.CurrentPenMode switch
				{
					Core.PenMode.Block => Text switch
					{
						EmptyText => BlockText,
						BlockText => FillText,
						_ => EmptyText
					},
					Core.PenMode.Fill => Text switch
					{
						EmptyText => BlockText,
						BlockText => FillText,
						_ => EmptyText
					},
					_ => Text
				};
			};
		}
	}
}
