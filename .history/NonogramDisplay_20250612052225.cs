using Godot;

namespace RSG.UI;

public abstract partial class NonogramDisplay : Container
{
	public enum HintType { Row = 1, Column = 0 }
	public const string BlockText = "X", FillText = "O", EmptyText = " ";
	private static Vector2 TileSize = new(40, 40);

	public int TilesLength
	{
		get; init
		{
			ChangeLength(value);
			field = value;
		}
	} = 5;

	public Tiles GridTiles { get; } = [];

	public GridContainer TilesContainer => field ??= new() { Name = "Tiles", Columns = TilesLength };
	public ColorRect Background { get; } = new() { Name = "Background" };
	public Control Spacer { get; } = new() { Name = "Spacer" };
	public GridContainer Main { get; } = new GridContainer { Name = "MainContainer", Columns = 2 }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public Hints GridHints { get; } = new();

	public override void _Ready()
	{
		this.Add(
				Background,
				Main.Add(Spacer, GridHints.Columns, GridHints.Rows, TilesContainer)
			)
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.BottomRight, resizeMode: LayoutPresetMode.KeepSize)
			.Name = nameof(NonogramDisplay);
		TilesContainer
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		Spacer
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		Background
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize)
			.CustomMinimumSize = Size;
		Main
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	}

	public abstract void OnTilePressed(Vector2I position, Button button);
	public virtual void UpdateSettings(IColours colours, IConfig config)
	{
		Background
			.UniformPadding(IConfig.Margin)
			.Color = colours.NonogramBackground;

		TilesContainer.CustomMinimumSize = config.TilesSize;
	}

	private int ChangeLength(int value)
	{
		value = value < 1 ? 2 : value;
		Tiles.FreeAll();

		foreach (Vector2I position in (Vector2I.One * value).AsRange())
		{
			var button = Tiles[position] = Tile();
			TilesContainer.AddChild(button);
			button.Pressed += () => OnTilePressed(position, button);

			Button Tile() => new Button
			{
				Name = $"Button {position}",
				Text = EmptyText,
				CustomMinimumSize = TileSize
			}
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		}


		GridHints.PlaceLabels(value);

		return TilesContainer.Columns = value;
	}

	public sealed record Tiles
	{
		public required GridContainer Container { get; init; };

		private Dictionary<Vector2I, Button> Buttons { get; } = [];

		public void FreeAll() => Buttons.FreeAll(Container);
		public void PlaceButtons(int length, Action pressed)
		{
			foreach (Vector2I position in (Vector2I.One * length).AsRange())
			{
				var button = Buttons[position] = Tile();
				TilesContainer.AddChild(button);
				button.Pressed += pressed;

				Button Tile() => new Button
				{
					Name = $"Button {position}",
					Text = EmptyText,
					CustomMinimumSize = TileSize
				}
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
				.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
			}
		}
	}
	public sealed record Hints
	{
		public BoxContainer Rows { get; } = new VBoxContainer { Name = "RowHints" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public BoxContainer Columns { get; } = new HBoxContainer { Name = "ColumnHints" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

		public Dictionary<HintType, RichTextLabel[]> Labels { get; set; } = new([
			new (HintType.Row, []),
			new (HintType.Column, [])
		]);

		public void PlaceLabels(int length)
		{
			foreach (int i in Range(0, count: length))
			{
				Rows.Add(Labels[HintType.Row][i] = Hint());
				Columns.Add(Labels[HintType.Column][i] = Hint());

				static RichTextLabel Hint() => new RichTextLabel
				{
					Text = "0",
					FitContent = true,
					CustomMinimumSize = TileSize
				}
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
			}
		}
	}

	public interface IColours
	{
		Color NonogramBackground { get; }
	}
	public interface IConfig
	{
		public static int Margin { get; } = 150;

		int Length { get; }
		int Scale { get; }

		Vector2I Size => Vector2I.One * Length;
		Vector2I TilesSize => Size * Scale;
		Vector2I BackgroundSize => Size * (Scale + 5);
	}
}
