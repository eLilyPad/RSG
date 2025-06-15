using Godot;

namespace RSG.UI;

using HintContainers = (BoxContainer Rows, BoxContainer Columns);

public static class NonogramExstensions
{
}

public abstract partial class NonogramDisplay : Container
{
	public const string BlockText = "X", FillText = "O", EmptyText = " ";

	public Dictionary<Vector2I, Button> Buttons { get; } = [];
	public Dictionary<Vector2I, RichTextLabel> Hints { get; } = [];

	public abstract GridContainer Tiles { get; }
	public abstract ColorRect Background { get; }
	public abstract Control Spacer { get; }

	public HintContainers HintContainers { get; } = (
		new VBoxContainer { Name = "RowHints" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize),
		new HBoxContainer { Name = "ColumnHints" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize)
	);
	public GridContainer Main { get; } = new GridContainer { Name = "MainContainer", Columns = 2 }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public override void _Ready()
	{
		Name = nameof(NonogramDisplay);
		this.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);

		Tiles.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		Spacer.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		Background.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

		this.Add(
			Background,
			Main.Add(Spacer, HintContainers.Columns, HintContainers.Rows, Tiles)
		);

		Background.CustomMinimumSize = Size;

		Vector2I size = Vector2I.One * Tiles.Columns;
		foreach (Vector2I position in size.AsRange())
		{
			var button = Buttons[position] = new Button { Name = $"Button {position}", Text = EmptyText }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
				.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
			Tiles.AddChild(button);
			button.Pressed += () => OnTilePressed(position, button);
		}
	}

	public abstract void OnTilePressed(Vector2I position, Button button);
	public abstract void UpdateSettings();
}
