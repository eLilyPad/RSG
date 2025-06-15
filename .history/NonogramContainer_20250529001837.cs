using Godot;

namespace RSG.UI;

public sealed partial class NonogramContainer : Container
{
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
					Text = "x",
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
					SizeFlagsVertical = SizeFlags.ExpandFill
				}.AnchorsAndOffsetsPreset(
					preset: LayoutPreset.FullRect,
					resizeMode: LayoutPresetMode.KeepSize
				);
				Grid.Add(_buttons[new(x, y)] = button);
			}
		}

		AddChild(Grid);
	}

	public sealed partial class GridButton
}
