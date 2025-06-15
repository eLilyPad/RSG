using Godot;

namespace RSG.UI;

using HintContainers = (BoxContainer Rows, BoxContainer Columns);

public abstract partial class NonogramDisplay : Container
{
	public const string BlockText = "X", FillText = "O", EmptyText = " ";

	public Dictionary<Vector2I, Button> Buttons { get; } = [];
	public Dictionary<Vector2I, RichTextLabel> Hints { get; } = [];

	public GridContainer Tiles { get; } = new() { Name = "Tiles" };
	public ColorRect Background { get; } = new();
	public HintContainers HintContainers { get; init; } = (new(), new());
	public Control Spacer => field ??= new();
	public GridContainer Main => field ??= new();

	public override void _Ready()
	{
		Name = nameof(NonogramDisplay);
		(SizeFlagsHorizontal, SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		Init(Main, Background, Spacer, HintContainers.Columns, HintContainers.Rows, Tiles);

		// Init Tile buttons
		Vector2I size = Vector2I.One * Tiles.Columns;
		foreach (Vector2I position in size.AsRange())
		{
			var button = Buttons[position] = new Button
			{
				Name = $"Button {position}",
				Text = EmptyText
			};
			Init(value: button).Pressed += () => OnTilePressed(position, button);
		}
		UpdateSettings();

	}

	public abstract void OnTilePressed(Vector2I position, Button button);
	public abstract void UpdateSettings();
	private void Init(params Span<Control> values)
	{
		foreach (var control in values)
		{
			_ = Init(value: control);
		}
	}
	private T Init<T>(T value) where T : Control
	{
		switch (value)
		{
			case Button tile:
				Tiles.AddChild(tile);
				break;
			case GridContainer tiles when tiles == Tiles:
				Main.AddChild(tiles);
				break;
			case GridContainer main when main == Main:
				(main.Name, main.Columns) = ("MainContainer", 2);
				main.SetAnchorsAndOffsetsPreset(
					preset: LayoutPreset.FullRect,
					resizeMode: LayoutPresetMode.KeepSize
				);
				AddChild(main);
				break;
			case ColorRect background when background == Background:
				background.Name = "Background";
				AddChild(background);
				break;
			case BoxContainer hints:
				hints.Name = hints switch
				{
					_ when hints == HintContainers.Rows => "RowHints",
					_ when hints == HintContainers.Columns => "ColumnsHints",
					_ => ""
				};
				Main.AddChild(hints);
				break;
			case Control spacer when spacer == Spacer:
				(spacer.Name, spacer.Size) = ("Spacer", Tiles.Size);
				Main.AddChild(spacer);
				break;
		}

		(value.SizeFlagsHorizontal, value.SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		value.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);
		return value;
	}
}
