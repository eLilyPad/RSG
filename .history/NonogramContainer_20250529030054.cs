using Godot;

namespace RSG.UI;

public sealed partial class NonogramContainer : Container
{
	private const int GridSize = 5;
	private const string BlockText = "X", FillText = "O", EmptyText = " ";

	public Core.PenMode PenMode { get; set; } = Core.PenMode.Block;
	public required ColourPack Colours { get; init; }
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
		Name = nameof(NonogramContainer);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);
		var background = new ColorRect
		{
			Name = "Margin Background",
			Color = Colours.NonogramBackground,
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
				CreateButton(x, y);
			}
		}

	}

	private Button CreateButton(int x, int y)
	{
		var button = new Button
		{
			Name = $"Button {x}, {y}",
			Text = EmptyText,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		button.Pressed += () =>
		{
			button.Text = PenMode switch
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
		};

		Grid.Add(_buttons[new(x, y)] = button);

		return button;
	}
}
