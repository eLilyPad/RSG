using Godot;

namespace RSG.UI;

using HintContainers = (BoxContainer Rows, BoxContainer Columns);

public abstract partial class NonogramDisplay : Container
{
	public const string BlockText = "X", FillText = "O", EmptyText = " ";

	public Dictionary<Vector2I, Button> Buttons { get; } = [];
	public Dictionary<Vector2I, RichTextLabel> Hints { get; } = [];

	public GridContainer Tiles { get; init => field = InitTiles(value); } = new();
	public ColorRect Background { get; init => field = InitBackGround(value); } = new();
	public HintContainers HintContainers { get; init => field = InitContainers(value); } = (
		new() { Name = "RowHints" }, new() { Name = "ColumnHints" }
	);
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

		this.Add(
			Background,
			Main.Add(Spacer, HintContainers.Rows, HintContainers.Columns, Tiles)
		);

		// Init Tile buttons
		Vector2I size = Vector2I.One * Tiles.Columns;
		foreach (Vector2I position in size.AsRange())
		{
			var button = Buttons[position] = new Button
			{
				Name = $"Button {position}",
				Text = EmptyText,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			}.AnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);
			AddChild(button);

			button.Pressed += () => OnTilePressed(position, button);
		}
	}

	public abstract void OnTilePressed(Vector2I position, Button button);
	public virtual void Init()
	{
		Background.Name = "Background";
		Tiles.Name = "Tiles";
		Init(Tiles);
		Init(HintContainers.Columns);
		Init(HintContainers.Rows);

		static void Init<TContainer>(TContainer value) where TContainer : Container
		{
			(value.SizeFlagsHorizontal, value.SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
			value.SetAnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);
		}
	}
	protected virtual T InitBackGround<T>(T value) where T : ColorRect
	{
		value.Name = "Background";
		return value;
	}
	protected virtual T InitTiles<T>(T value) where T : GridContainer
	{
		value.Name = "Tiles";
		(value.SizeFlagsHorizontal, value.SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		value.SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);
		return value;
	}
	protected virtual (T columns, T rows) InitContainers<T>((T columns, T rows) value) where T : BoxContainer
	{
		return (Init(value.columns), Init(value.rows));
		static T Init(T value)
		{
			(value.SizeFlagsHorizontal, value.SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
			value.SetAnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);
			return value;
		}
	}
}
