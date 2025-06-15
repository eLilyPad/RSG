using Godot;

namespace RSG.UI;

using HintContainers = (BoxContainer Rows, BoxContainer Columns);

public static class NonogramExstensions
{
}

public abstract partial class NonogramDisplay : Container
{
	public const string BlockText = "X", FillText = "O", EmptyText = " ";

	public int TilesLength
	{
		get; init
		{
			foreach (var (position, tile) in Tiles)
			{
				TilesContainer.RemoveChild(tile);
				Tiles.Remove(position);
				tile.QueueFree();
			}

			TilesContainer.Columns = field = value;

			foreach (Vector2I position in (Vector2I.One * value).AsRange())
			{
				var button = Tiles[position] = new Button { Name = $"Button {position}", Text = EmptyText }
					.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
					.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
				TilesContainer.AddChild(button);
				button.Pressed += () => OnTilePressed(position, button);
			}
		}
	} = 5;

	public Dictionary<Vector2I, Button> Tiles { get; } = [];
	public Dictionary<Vector2I, RichTextLabel> Hints { get; } = [];

	public GridContainer TilesContainer { get; } = new() { Name = "Tiles", Columns = 2 };
	public ColorRect Background { get; } = new() { Name = "Background" };
	public Control Spacer { get; } = new() { Name = "Spacer" };
	public GridContainer Main { get; } = new GridContainer { Name = "MainContainer", Columns = 2 }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public HintContainers HintContainers { get; } = (
		new VBoxContainer { Name = "RowHints" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize),
		new HBoxContainer { Name = "ColumnHints" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize)
	);

	public override void _Ready()
	{
		Name = nameof(NonogramDisplay);

		this.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
		TilesContainer.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		Spacer.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		Background.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize)
			.CustomMinimumSize = Size;

		this.Add(
			Background,
			Main.Add(Spacer, HintContainers.Columns, HintContainers.Rows, TilesContainer)
		);

	}

	public abstract void OnTilePressed(Vector2I position, Button button);
	public abstract void UpdateSettings();
}
