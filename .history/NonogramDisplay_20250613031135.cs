using Godot;

namespace RSG.UI;

public abstract partial class NonogramDisplay : Container
{
	public const string BlockText = "X", FillText = "O", EmptyText = " ";
	private static Vector2 TileSize = new(40, 40);

	public int TilesLength
	{
		get; init
		{
			value = value < 1 ? 2 : value;
			Tiles.FreeAll();
			Tiles.PlaceButtons(length: value, pressed: OnTilePressed);
			Tiles.Columns = field = value;
		}
	} = 5;

	public TilesContainer Tiles => field ??= new() { Name = "Tiles", Columns = TilesLength };
	public TileHints Hints => field ??= new(length: TilesLength);

	public ColorRect Background { get; } = new() { Name = "Background" };
	public Control Spacer { get; } = new() { Name = "Spacer" };
	public GridContainer Main { get; } = new() { Name = "MainContainer", Columns = 2 };

	public override void _Ready()
	{
		this.Add(
				Background,
				Main.Add(Spacer, Hints.Columns, Hints.Rows, Tiles)
			)
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.BottomRight, resizeMode: LayoutPresetMode.KeepSize)
			.Name = nameof(NonogramDisplay);
		Tiles
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
		Tiles.CustomMinimumSize = config.TilesSize;
		Background
			.UniformPadding(IConfig.Margin)
			.Color = colours.NonogramBackground;
	}

	public sealed partial class TilesContainer : GridContainer
	{
		public Dictionary<Vector2I, Button> Buttons { get; } = [];

		public void FreeAll() => Buttons.FreeAll(this);
		public void PlaceButtons(int length, Action<Button> pressed)
		{
			foreach (Vector2I position in (Vector2I.One * length).AsRange())
			{
				_ = CreateButton(position, pressed);
			}
		}

		private Button CreateButton(Vector2I position, Action<Button> pressed)
		{
			var button = new Button { Name = $"Button {position}", Text = EmptyText, CustomMinimumSize = TileSize }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
				.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
			AddChild(Buttons[position] = button);
			button.Pressed += () => pressed(button);
			return button;
		}
	}
	public sealed record TileHints
	{
		public readonly record struct Position(Side Side, int Index);
		public enum Side { Row, Column }

		public BoxContainer Rows { get; } = new VBoxContainer { Name = "RowHints" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public BoxContainer Columns { get; } = new HBoxContainer { Name = "ColumnHints" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

		private Dictionary<Position, RichTextLabel> Labels { get; } = [];

		public TileHints(int length) { PlaceLabels(length); }

		public void PlaceLabels(int length)
		{
			foreach (int i in Range(0, count: length))
			{
				Rows.Add(CreateHint(new(Side: Side.Row, Index: i)));
				Columns.Add(CreateHint(new(Side: Side.Column, Index: i)));
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

		private RichTextLabel CreateHint(Position position)
		{
			var label = new RichTextLabel { Text = "0", FitContent = true, CustomMinimumSize = TileSize }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
			Labels[position] = label;
			return label;
		}
	}

	public interface IColours { Color NonogramBackground { get; } }
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
