using Godot;

namespace RSG.UI;

public abstract partial class NonogramDisplay : Container
{
	public enum HintSide { Row = 1, Column = 0 }
	public const string BlockText = "X", FillText = "O", EmptyText = " ";
	private static Vector2 TileSize = new(40, 40);

	public int TilesLength
	{
		get; init
		{
			value = value < 1 ? 2 : value;
			GridTiles.FreeAll();
			GridTiles.PlaceButtons(length: value, pressed: OnTilePressed);
			GridHints.PlaceLabels(length: value);

			GridTiles.Columns = field = value;

		}
	} = 5;

	public Tiles GridTiles => field ??= new() { Name = "Tiles", Columns = TilesLength };

	public ColorRect Background { get; } = new() { Name = "Background" };
	public Control Spacer { get; } = new() { Name = "Spacer" };
	public GridContainer Main { get; } = new() { Name = "MainContainer", Columns = 2 };
	public Hints GridHints { get; } = new();

	public override void _Ready()
	{
		this.Add(
				Background,
				Main.Add(Spacer, GridHints.ColumnsContainer, GridHints.RowsContainer, GridTiles)
			)
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.BottomRight, resizeMode: LayoutPresetMode.KeepSize)
			.Name = nameof(NonogramDisplay);
		GridTiles
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

	public abstract void OnTilePressed(Button button);
	public virtual void UpdateSettings(IColours colours, IConfig config)
	{
		Background
			.UniformPadding(IConfig.Margin)
			.Color = colours.NonogramBackground;

		GridTiles.CustomMinimumSize = config.TilesSize;
	}

	public sealed partial class Tiles : GridContainer
	{
		public Dictionary<Vector2I, Button> Buttons { get; } = [];

		public void FreeAll() => Buttons.FreeAll(this);
		public void PlaceButtons(int length, Action<Button> pressed)
		{
			foreach (Vector2I position in (Vector2I.One * length).AsRange())
			{
				var button = Buttons[position] = Tile();
				AddChild(button);
				button.Pressed += () => pressed(button);

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
		public readonly record struct Position(Side Side, int Index);
		public enum Side { Row = 1, Column = 0 }

		public BoxContainer RowsContainer { get; } = new VBoxContainer { Name = "RowHints" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public BoxContainer ColumnsContainer { get; } = new HBoxContainer { Name = "ColumnHints" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

		private Dictionary<Position, RichTextLabel> Labels { get; } = [];

		public void PlaceLabels(int length)
		{
			foreach (int i in Range(0, count: length))
			{
				RowsContainer.Add(Labels[new(Side: Side.Row, Index: i)] = Hint());
				ColumnsContainer.Add(Labels[new(Side: Side.Column, Index: i)] = Hint());

				static RichTextLabel Hint() => new RichTextLabel
				{
					Text = "0",
					FitContent = true,
					CustomMinimumSize = TileSize
				}
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
			}
		}

		public void WriteToLabel(Position position, string text)
		{
			Assert(
				condition: Labels.Count <= position.Index || position.Index < 0,
				message: $"position is not within range, must be <= {Labels.Count} and < 0, given position is {position}"
			);
			Labels[position].Text = text;
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
