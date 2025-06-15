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
	public HintContainers HintContainers { get; init; } = (new(), new());
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
		Init(Background, Tiles, HintContainers.Columns, HintContainers.Rows);

		this.Add(
			Background,
			Main.Add(Spacer, HintContainers.Rows, HintContainers.Columns, Tiles)
		);

		// Init Tile buttons
		Vector2I size = Vector2I.One * Tiles.Columns;
		foreach (Vector2I position in size.AsRange())
		{
			var button = Buttons[position] = new Button { Name = $"Button {position}" };
			Init(button);
			button.Pressed += () => OnTilePressed(position, button);
		}
		UpdateSettings();

	}

	public abstract void OnTilePressed(Vector2I position, Button button);
	public abstract void UpdateSettings();
	private void Init(params Span<Control> values)
	{
		foreach (Control value in values)
		{
			Init(value);
		}
	}
	private void Init(Control value)
	{
		switch (value)
		{
			case Button tile:
				tile.Text = EmptyText;
				Tiles.AddChild(tile);
				break;
			case GridContainer tiles:
				tiles.Name = "Tiles";
				break;
			case ColorRect background:
				background.Name = "Background";
				break;
			case BoxContainer hints:
				hints.Name = hints switch
				{
					_ when hints == HintContainers.Rows => "RowHints",
					_ when hints == HintContainers.Columns => "ColumnsHints",
					_ => ""
				};
				break;
		}

		(value.SizeFlagsHorizontal, value.SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		value.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);
	}
}
