using Godot;

namespace RSG.UI;

public sealed partial class Nonogram : Container
{
	public static Nonogram Create<T>(T data) where T : IHavePenMode, IHaveColourPack
	{
		Nonogram nonogram = new()
		{
			Name = nameof(Nonogram),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			Tiles = TilesContainer.Create(data)
		};
		return nonogram;
	}

	public required Container Tiles { get; init; }
	public HintsContainer ColumnHints => field ??= new HintsContainer { MaxHints = MaxLength };
	public HintsContainer RowHints => field ??= new HintsContainer { MaxHints = MaxLength };
	public Control Spacer => field ??= new Control { Name = "Spacer", Size = Tiles.Size };
	public GridContainer Grid => field ??= new GridContainer
	{
		Name = "Grid",
		Columns = MaxLength,
		Size = Tiles.Size
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.BottomRight,
		resizeMode: LayoutPresetMode.KeepSize
	);

	private int MaxLength { get; } = 5;

	private Nonogram() { }

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		this.Add(
			Grid.Add(Spacer, RowHints, ColumnHints, Tiles)
		);
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
	public static TilesContainer Create<T>(
		T data,
		int size = 5,
		int scale = 40
	) where T : IHavePenMode, IHaveColourPack
	{
		int margin = 150;
		Vector2I tilesSize = Vector2I.One * size;
		var background = new ColorRect
		{
			Name = "Background",
			Color = data.Colours.NonogramBackground,
			Size = tilesSize * (scale + 5)
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.BottomRight,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: margin
		);
		var tilesContainer = new TilesContainer
		{
			Background = background,
			Columns = size,
			Name = "Tiles",
			Size = tilesSize * scale,
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
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.BottomRight,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: margin
		);

		return tilesContainer;
	}

	public const string BlockText = "X", FillText = "O", EmptyText = " ";

	public required Action<Button> OnButtonPressed { get; init; }

	public required ColorRect Background { get; init; }
	public Dictionary<Vector2I, Button> Buttons { get; } = [];

	private TilesContainer() { }

	public override void _Ready()
	{
		this.Add(Background);

		foreach (Vector2I position in (Vector2I.One * GridLength).AsRange())
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
