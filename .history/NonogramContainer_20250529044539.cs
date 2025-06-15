using Godot;

namespace RSG.UI;

public sealed partial class Nonogram<T> : Container where T : IHavePenMode, IHaveColourPack
{
	private const int GridSize = 5;
	private const string BlockText = "X", FillText = "O", EmptyText = " ";
	private static Vector2I GridSizeVector = Vector2I.One * GridSize * 40;

	public required T Data { get; init; }
	public TilesContainer Tiles => field ??= new TilesContainer(Data);

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

		this.Add(Tiles);
	}

	public sealed partial class TilesContainer : Container
	{
		public GridContainer Grid => field ??= new GridContainer()
		{
			Name = "Grid",
			Columns = GridSize,
			Size = Vector2I.One * GridSize * 40
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.CenterRight,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: 150
		);

		public ColorRect Background => field ??= new ColorRect
		{
			Name = "Background",
			Size = Vector2I.One * GridSize * 40
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.CenterRight,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: 150
		);

		public Dictionary<Vector2I, Button> Buttons { get; } = [];

		public TilesContainer(T data)
		{
			AddChild(Grid);

			for (int x = 0; x < GridSize; x++)
			{
				for (int y = 0; y < GridSize; y++)
				{
					Vector2I position = new(x, y);
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
	}
}
