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

		this.Add(
			new NonogramBackground(colours: Data.Colours),
			Grid
		);

		for (int x = 0; x < GridSize; x++)
		{
			for (int y = 0; y < GridSize; y++)
			{
				_buttons[new(x, y)] = new NonogramButton(data: Data, parent: Grid, x, y);
			}
		}
	}

	private sealed partial class NonogramBackground : ColorRect
	{
		public NonogramBackground(ColourPack colours)
		{
			Name = "Margin Background";
			Color = colours.NonogramBackground;
			SizeFlagsHorizontal = SizeFlags.ExpandFill;
			SizeFlagsVertical = SizeFlags.ExpandFill;

			SetAnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);
		}
	}
	private sealed partial class NonogramButton : Button
	{
		public NonogramButton(T data, Node parent, int x, int y)
		{
			Name = $"Button {x}, {y}";
			Text = EmptyText;
			SizeFlagsHorizontal = SizeFlags.ExpandFill;
			SizeFlagsVertical = SizeFlags.ExpandFill;

			SetAnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);

			parent.AddChild(this);
			Pressed += () => Text = data.CurrentPenMode switch
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
		}
	}
}
