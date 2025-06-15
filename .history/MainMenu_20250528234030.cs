using Godot;

namespace RSG.UI;

public sealed partial class ButtonGrid : GridContainer
{
	public ButtonGrid()
	{
		Name = nameof(ButtonGrid);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);
	}
}

public sealed partial class NonogramContainer : Container
{
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
				_buttons[x][y] = button;
				this.Add(button);
			}
		}
	}
}
public sealed partial class MainMenu : Container
{
	public sealed partial class Buttons : VBoxContainer
	{
		public Buttons()
		{
			Name = nameof(Buttons);
			SizeFlagsHorizontal = SizeFlags.ExpandFill;
			SizeFlagsVertical = SizeFlags.ExpandFill;

			SetAnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);
		}
	}

	public required ColourPack Colours { get; init; }

	public override void _Ready()
	{
		Name = nameof(MainMenu);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			100
		);

		var background = new ColorRect
		{
			Name = "Margin Background",
			Color = Colours.MainMenuBackground,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		this.Add(background);
	}
}