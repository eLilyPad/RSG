using Godot;

namespace RSG.UI;

public sealed partial class Nonogram<T> : Container where T : IHavePenMode, IHaveColourPack
{
	private const int GridSize = 5;
	private const string BlockText = "X", FillText = "O", EmptyText = " ";
	private static Vector2I GridSizeVector = Vector2I.One * GridSize * 40;

	public required T Data { get; init; }

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

		this.Add(GridBackground(), Grid());
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
				grid.AddChild(node: GridButton(position));
			}
		}

		return grid;
	}
	private Button GridButton(Vector2I position)
	{
		if (_buttons.TryGetValue(position, value: out Button? existingButton))
		{
			return existingButton;
		}

		var button = new Button
		{
			Name = $"Button {position}",
			Text = EmptyText,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		button.Pressed += () => button.Text = Data.CurrentPenMode switch
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

		return _buttons[position] = button;
	}

	private static ColorRect GridBackground()
	{
		return new ColorRect
		{
			Name = "Background",
			Size = Vector2I.One * GridSize * 40
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.CenterRight,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: 150
		);

	}

	private sealed partial class Tiles : Container
	{
		public GridContainer Grid => field ??= CreateGrid();
		public Dictionary<Vector2I, Button> Buttons { get; } = [];

		private GridContainer CreateGrid()
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
					grid.AddChild(node: GridButton(position));
				}
			}
			AddChild(grid);

			return grid;
		}
		private Button GridButton(Vector2I position)
		{
			if (Buttons.TryGetValue(position, value: out Button? existingButton))
			{
				return existingButton;
			}

			var button = new Button
			{
				Name = $"Button {position}",
				Text = EmptyText,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			}.AnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);

			button.Pressed += () => button.Text = Data.CurrentPenMode switch
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

			return Buttons[position] = button;
		}
	}
}
