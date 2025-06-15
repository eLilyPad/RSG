using Godot;

namespace RSG.UI;

public sealed partial class Nonogram<T>(T data) : Container where T : IHavePenMode, IHaveColourPack
{
	public Container Tiles => field ??= TilesContainer.Create(data);
	public HintsContainer ColumnHintsContainer => field ??= new HintsContainer { MaxHints = TilesContainer.GridLength };
	public HintsContainer RowHintsContainer => field ??= new HintsContainer { MaxHints = TilesContainer.GridLength };
	public Control Spacer => field ??= new Control { Name = "Spacer", Size = Tiles.Size };
	public GridContainer Grid => field ??= new GridContainer
	{
		Name = "Grid",
		Columns = TilesContainer.GridLength,
		Size = Tiles.Size
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.BottomRight,
		resizeMode: LayoutPresetMode.KeepSize,
		margin: TilesContainer.GridMargin
	);

	public override void _Ready()
	{
		Name = nameof(Nonogram<>);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		this.Add(Grid.Add(
			Spacer,
			RowHintsContainer,
			ColumnHintsContainer,
			Tiles
		));
	}
}
public sealed partial class HintsContainer : BoxContainer
{
	public required int MaxHints
	{
		get; init => (_hints, field) = (new List<RichTextLabel>[value], value);
	}

	private readonly List<RichTextLabel>[] _hints = [];

	public HintsContainer()
	{
		Name = "Hints";
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.TopLeft,
			resizeMode: LayoutPresetMode.KeepSize
		);
	}

	public override void _Ready()
	{
		for (int i = 0; i < TilesContainer.GridLength; i++)
		{
			RichTextLabel hint = new RichTextLabel
			{
				Name = $"Row Hint {i}",
				Text = "0",
				SizeFlagsStretchRatio = 0.3f,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			}
			.AnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);

			switch (_hints[i])
			{
				case List<RichTextLabel> hints:
					hints.Add(hint);
					break;
				default:
					_hints[i] = [];
					break;
			}
			this.Add(hint);
		}
	}
}

public sealed partial class TilesContainer : GridContainer
{
	public static TilesContainer Create<T>(T data) where T : IHavePenMode, IHaveColourPack
	{
		ColorRect background = new ColorRect
		{
			Name = "Background",
			Color = data.Colours.NonogramBackground,
			Size = GridSize * (GridScale + 5)
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.BottomRight,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: GridMargin
		);
		TilesContainer tilesContainer = new()
		{
			Background = background,
			Columns = GridLength,
			Name = "Tiles",
			Size = GridSize * GridScale,
			OnButtonPressed = button =>
			{
				button.Text = data.CurrentPenMode switch
				{
					Core.PenMode.Block => button.Text switch
					{
						EmptyText => BlockText,
						BlockText => FillText,
						_ => EmptyText
					},
					Core.PenMode.Fill => button.Text switch
					{
						EmptyText => BlockText,
						BlockText => FillText,
						_ => EmptyText
					},
					_ => button.Text
				};
			}
		};

		return tilesContainer;
	}

	public const string BlockText = "X", FillText = "O", EmptyText = " ";
	public const int GridScale = 40, GridMargin = 150, GridLength = 5;
	public static Vector2I GridSize => Vector2I.One * GridLength;

	public required Action<Button> OnButtonPressed { get; init; }

	public required ColorRect Background { get; init; }
	public Dictionary<Vector2I, Button> Buttons { get; } = [];

	private TilesContainer() { }

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.BottomRight,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: GridMargin
		);

		foreach (Vector2I position in GridSize.AsRange())
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

			button.Pressed += () => OnButtonPressed(button);
		}
	}
}
