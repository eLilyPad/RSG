using Godot;

namespace RSG.UI;

using HintContainers = (BoxContainer Rows, BoxContainer Columns);

public abstract partial class NonogramDisplay : Container
{
	public const string BlockText = "X", FillText = "O", EmptyText = " ";

	public Dictionary<Vector2I, Button> Buttons { get; } = [];
	public Dictionary<Vector2I, RichTextLabel> Hints { get; } = [];

	public GridContainer Tiles { get; } = new();
	public ColorRect Background { get; } = new();
	public HintContainers HintContainers { get; init; } = (new() { Name = "RowHints" }, new() { Name = "ColumnHints" });
	public Control Spacer => field ??= new Control { Name = "Spacer", Size = Tiles.Size };
	public GridContainer Main => field ??= new GridContainer
	{
		Name = "MainContainer",
		Columns = 2,
		Size = Tiles.Size
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.FullRect,
		resizeMode: LayoutPresetMode.KeepSize
	);

	public override void _Ready()
	{
		Name = nameof(NonogramDisplay);
		(SizeFlagsHorizontal, SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		Background.Name = "Background";
		Init(Tiles).Name = "Tiles";
		Init(HintContainers.Columns);
		Init(HintContainers.Rows);

		this.Add(
			Background,
			Main.Add(Spacer, HintContainers.Rows, HintContainers.Columns, Tiles)
		);

		// Init Tile buttons
		Vector2I size = Vector2I.One * Tiles.Columns;
		foreach (Vector2I position in size.AsRange())
		{
			var button = Buttons[position] = Init(new Button { Name = $"Button {position}", Text = EmptyText });
			AddChild(button);

			button.Pressed += () => OnTilePressed(position, button);
		}
		UpdateSettings();

		static TContainer Init<TContainer>(TContainer value) where TContainer : Container
		{
			(value.SizeFlagsHorizontal, value.SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
			return value.AnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);
		}
	}

	public abstract void OnTilePressed(Vector2I position, Button button);
	public abstract void UpdateSettings();
}
