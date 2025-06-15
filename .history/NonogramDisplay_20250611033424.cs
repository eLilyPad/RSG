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
		get; set
		{
			ClearTiles();
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

	public virtual GridContainer TilesContainer { get; } = new() { Name = "Tiles", Columns = 2 };
	public virtual ColorRect Background { get; } = new() { Name = "Background" };
	public virtual Control Spacer { get; } = new() { Name = "Spacer" };

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

		//PlaceButtons(Tiles.Columns);
	}

	public abstract void OnTilePressed(Vector2I position, Button button);
	public abstract void UpdateSettings();
	public virtual void PlaceTiles(int length)
	{
		Assert(length > 1, "Puzzles Length is lower the required 2 or more");

		ClearTiles();
		TilesContainer.Columns = length;

		foreach (Vector2I position in (Vector2I.One * length).AsRange())
		{
			var button = Tiles[position] = new Button { Name = $"Button {position}", Text = EmptyText }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
				.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
			TilesContainer.AddChild(button);
			button.Pressed += () => OnTilePressed(position, button);
		}
	}

	private void ClearTiles()
	{
		foreach (var (position, tile) in Tiles)
		{
			TilesContainer.RemoveChild(tile);
			Tiles.Remove(position);
			tile.QueueFree();
		}
	}
}
