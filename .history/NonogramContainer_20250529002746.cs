using Godot;

namespace RSG.UI;

public sealed partial class NonogramContainer : Container
{
	public Core.PenMode PenMode { get; set; } = Core.PenMode.Block;

	public GridContainer Grid { get; } = new GridContainer
	{
		Name = "Grid",
		Columns = GridSize,
		SizeFlagsHorizontal = SizeFlags.ExpandFill,
		SizeFlagsVertical = SizeFlags.ExpandFill
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.FullRect,
		resizeMode: LayoutPresetMode.KeepSize
	);

	private readonly Dictionary<Vector2I, Button> _buttons = [];
	private const int GridSize = 5;
	private const string BlockText = "X", FillText = "O", EmptyText = " ";

	public override void _Ready()
	{
		Name = nameof(NonogramContainer);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		for (int x = 0; x < GridSize; x++)
		{
			for (int y = 0; y < GridSize; y++)
			{
				var button = new Button
				{
					Name = $"Button {x},{y}",
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
						Core.PenMode.Block when button.Text is BlockText => EmptyText,
						Core.PenMode.Fill when button.Text is FillText => EmptyText,
						_ => button.Text
					};
				};

				Grid.Add(_buttons[new(x, y)] = button);
			}
		}

		AddChild(Grid);
	}

	public sealed partial class GridButton : Button { }
}
