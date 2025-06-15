using Godot;

namespace RSG.UI;

public sealed partial class Nonogram<T> : Container where T : IHavePenMode, IHaveColourPack
{
	private const int GridSize = 5;
	private const string BlockText = "X", FillText = "O", EmptyText = " ";

	public required T Data { get; init; }
	public ColorRect BackgroundRect => field ??= new Background(colours: Data.Colours);

	private readonly Dictionary<Vector2I, Button> _buttons = [];

	public override void _Ready()
	{
		Name = nameof(Nonogram<>);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		this.Add(new Background(colours: Data.Colours), Grid());
	}

	private ColorRect Background()
	{
		return new ColorRect()
		{
			Name = "Background",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.CenterRight,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: 150
		);

	}
	private GridContainer Grid()
	{
		var grid = new GridContainer()
		{
			Name = "Grid",
			Columns = GridSize,
			Size = Vector2I.One * GridSize * 40
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.CenterRight,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: 150
		);

		for (int x = 0; x < GridSize; x++)
		{
			for (int y = 0; y < GridSize; y++)
			{
				Vector2I position = new(x, y);
				var button = _buttons[position] = new NonogramButton(Data, x, y);
				AddChild(button);
			}
		}

		return grid;
	}

	private sealed partial class NonogramGrid : GridContainer
	{
		public Dictionary<Vector2I, Button> Buttons { get; } = [];

		public NonogramGrid(Nonogram<T> parent)
		{
			Name = "Grid";
			Columns = GridSize;
			Size = Vector2I.One * GridSize * 40;

			SetAnchorsAndOffsetsPreset(
				preset: LayoutPreset.CenterRight,
				resizeMode: LayoutPresetMode.KeepSize,
				margin: 150
			);

			for (int x = 0; x < GridSize; x++)
			{
				for (int y = 0; y < GridSize; y++)
				{
					Vector2I position = new(x, y);
					var button = Buttons[position] = new NonogramButton(parent.Data, x, y);
					AddChild(button);
				}
			}
		}
	}
	private sealed partial class Background : ColorRect
	{
		public Background(ColourPack colours)
		{
			Name = "Margin Background";
			Color = colours.NonogramBackground;
			SizeFlagsHorizontal = SizeFlags.ExpandFill;
			SizeFlagsVertical = SizeFlags.ExpandFill;

			SetAnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);
		}
	}
	private sealed partial class NonogramButton : Button
	{
		public NonogramButton(T data, int x, int y)
		{
			Name = $"Button {x}, {y}";
			Text = EmptyText;
			SizeFlagsHorizontal = SizeFlags.ExpandFill;
			SizeFlagsVertical = SizeFlags.ExpandFill;

			SetAnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);

			Pressed += () => Text = data.CurrentPenMode switch
			{
				Core.PenMode.Block => Text switch
				{
					EmptyText => BlockText,
					BlockText => FillText,
					_ => EmptyText
				},
				Core.PenMode.Fill => Text switch
				{
					EmptyText => BlockText,
					BlockText => FillText,
					_ => EmptyText
				},
				_ => Text
			};
		}
	}
}
