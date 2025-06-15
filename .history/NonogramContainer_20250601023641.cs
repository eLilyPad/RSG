using Godot;

namespace RSG.UI;

public sealed partial class Nonogram : Container
{
	public static Nonogram Create<T>(
		T data,
		int length = 5,
		int scale = 40,
		int margin = 150
	) where T : IHavePenMode, IHaveColourPack
	{
		Vector2I tilesSize = Vector2I.One * length;
		var background = new ColorRect
		{
			Name = "Background",
			Color = data.Colours.NonogramBackground,
			Size = tilesSize * (scale + 5)
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			margin
		);

		return new Nonogram
		{
			Name = nameof(Nonogram),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			Background = background,
			Tiles = TilesContainer.Create(data, length, scale, margin),
			Hints = (new HintsContainer(length), new HintsContainer(length)),
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			margin
		);
	}

	public required TilesContainer Tiles { get; init; }
	public required (HintsContainer Rows, HintsContainer Columns) Hints { get; init; }
	public required ColorRect Background { get; init; }
	public Control Spacer => field ??= new Control { Name = "Spacer", Size = Tiles.Size };
	public GridContainer Grid => field ??= new GridContainer
	{
		Name = "Grid",
		Columns = 2,
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
			Background,
			Grid.Add(Spacer, Hints.Rows, Hints.Columns, Tiles)
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
			SetHint(line: i, 0, "0");
		}
	}
	public void SetHint(int line, int position, string text)
	{
		if (line < 0 || line >= _hints.Length) { return; }
		RichTextLabel[] lines = _hints[line];
		if (position < 0 || position >= lines.Length) { return; }
		if (lines[position] is RichTextLabel label)
		{
			label.Text = text;
			return;
		}
		lines[position] = new RichTextLabel
		{
			Name = $"Hint ({line}, {position})",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);
		this.Add(lines[position]);
	}
}

public sealed partial class TilesContainer : GridContainer
{
	public static TilesContainer Create<T>(T data, int length, int scale, int margin)
	where T : IHavePenMode, IHaveColourPack
	{
		var tilesContainer = new TilesContainer
		{
			Name = "Tiles",
			Columns = length,
			Size = Vector2I.One * length * scale,
			OnButtonPressed = button =>
			{
				button.Text = data.CurrentPenMode switch
				{
					Core.PenMode.Block when button.Text is EmptyText or FillText => BlockText,
					Core.PenMode.Block => EmptyText,
					Core.PenMode.Fill when button.Text is EmptyText => BlockText,
					Core.PenMode.Fill when button.Text is FillText => EmptyText,
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

	public Dictionary<Vector2I, Button> Buttons { get; } = [];

	private TilesContainer() { }

	public override void _Ready()
	{
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
