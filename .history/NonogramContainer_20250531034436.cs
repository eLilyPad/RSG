using Godot;

namespace RSG.UI;

public sealed partial class Nonogram<T>(T data) : Container where T : IHavePenMode, IHaveColourPack
{
	public Container Tiles => field ??= new TilesContainer<T>(data)
	{
		OnButtonPressed = position =>
		{

		}
	};
	public HBoxContainer ColumnHintsContainer => field ??= new HBoxContainer
	{
		Name = "Column Hints",
		SizeFlagsHorizontal = SizeFlags.ExpandFill,
		SizeFlagsVertical = SizeFlags.ShrinkCenter
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.TopLeft,
		resizeMode: LayoutPresetMode.KeepSize,
		margin: TilesContainer<T>.GridMargin
	);
	public VBoxContainer RowHintsContainer => field ??= new VBoxContainer
	{
		Name = "Row Hints",
		SizeFlagsHorizontal = SizeFlags.ExpandFill,
		SizeFlagsVertical = SizeFlags.ShrinkCenter
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.TopLeft,
		resizeMode: LayoutPresetMode.KeepSize,
		margin: TilesContainer<T>.GridMargin
	);
	public GridContainer Grid => field ??= new GridContainer
	{
		Name = "Grid",
		Columns = TilesContainer<T>.GridLength,
		Size = TilesContainer<T>.GridSizeScaled
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.BottomRight,
		resizeMode: LayoutPresetMode.KeepSize,
		margin: TilesContainer<T>.GridMargin
	);
	public Control Spacer => field ??= new Control
	{
		Name = "Spacer",
		Size = TilesContainer<T>.GridSizeScaled
	};

	private readonly List<RichTextLabel>[] _columnHints = new List<RichTextLabel>[TilesContainer<T>.GridLength];
	private readonly List<RichTextLabel>[] _rowHints = new List<RichTextLabel>[TilesContainer<T>.GridLength];

	public override void _Ready()
	{
		Name = nameof(Nonogram<>);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);
		for (int i = 0; i < TilesContainer<T>.GridLength; i++)
		{
			RichTextLabel columnHint = new RichTextLabel
			{
				Name = $"Column Hint {i}",
				Text = "0",
				SizeFlagsStretchRatio = 0.3f,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			}
			.AnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);

			RichTextLabel rowHint = new RichTextLabel
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
			ColumnHintsContainer.Add(columnHint);
			RowHintsContainer.Add(rowHint);
		}

		this.Add(Grid.Add(
			Spacer,
			RowHintsContainer,
			ColumnHintsContainer,
			Tiles
		));
	}

	private void UpdateHints(Vector2I position)
	{

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

		return new TilesContainer(background)
		{
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
	}

	public const string BlockText = "X", FillText = "O", EmptyText = " ";
	public const int GridScale = 40, GridMargin = 150, GridLength = 5;
	public static Vector2I GridSize => Vector2I.One * GridLength;
	public static Vector2I GridSizeScaled => Vector2I.One * GridLength * GridScale;

	public required Action<Button> OnButtonPressed { get; init; }

	public ColorRect Background { get; init; }

	public Dictionary<Vector2I, Button> Buttons { get; } = [];

	private TilesContainer(ColorRect background)
	{
		Background = background;
	}
	public override void _Ready()
	{
		Name = "Tiles";
		Columns = GridLength;
		Size = GridSize * GridScale;
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

			button.Pressed += () => button.Text = data.CurrentPenMode switch
			button.Pressed += () => button.Text = data.CurrentPenMode switch
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
	}
}
