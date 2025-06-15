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
			Tiles = TilesContainer.Create(data),
			ColumnHints = new HintsContainer(length: 5),
			RowHints = new HintsContainer(length: 5),
		};
		return nonogram;
	}

	public required Container Tiles { get; init; }
	public required HintsContainer ColumnHints { get; init; }
	public HintsContainer RowHints { get; init; }
	public Control Spacer => field ??= new Control { Name = "Spacer", Size = Tiles.Size };
	public GridContainer Grid => field ??= new GridContainer
	{
		Name = "Grid",
		Columns = MaxLength,
		Size = Tiles.Size
	}.AnchorsAndOffsetsPreset(
		preset: LayoutPreset.FullRect,
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
	private readonly RichTextLabel[][] _hints = [];

	public HintsContainer(int length)
	{
		Name = "Hints";
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;
		_hints = new RichTextLabel[length][];

		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		for (int i = 0; i < length; i++)
		{
			_hints[i] = new RichTextLabel[length];
		}
	}

	public override void _Ready()
	{
		for (int i = 0; i < _hints.Length; i++)
		{
			RichTextLabel hint = _hints[i][0] = new RichTextLabel
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

			this.Add(hint);
		}
	}
	public void SetHint(int x, int y, string text)
	{
		if (x < 0 || x >= _hints.Length) { return; }
		if (y < 0 || y >= _hints[x].Length) { return; }

		_hints[x][y].Text = text;
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
			Name = "Tiles",
			Background = background,
			Columns = size,
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
		AddChild(Background);

		foreach (Vector2I position in (Vector2I.One * Columns).AsRange())
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
