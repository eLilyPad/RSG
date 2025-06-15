using Godot;

namespace RSG.UI;

public abstract partial class NonogramDisplay : Container
{
	public const string BlockText = "X", FillText = "O", EmptyText = " ";
	public enum PenMode { Block, Fill }
	private static Vector2 TileSize = new(40, 40);

	public abstract IConfig Config { get; }
	public IColours Colours { set => Background.Color = new(0, 0, 0); }
	public PenMode CurrentPenMode { get; set; } = PenMode.Fill;
	public TileHints Hints => field ??= new() { Config = Config };
	public GridContainer Tiles => field ??= new GridContainer
	{
		Name = "Tiles",
		Columns = Config.Length,
		CustomMinimumSize = Config.TilesSize
	}
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = new(0, 0, 0) }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public Control Spacer { get; } = new Control { Name = "Spacer" }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public GridContainer Main { get; } = new GridContainer { Name = "MainContainer", Columns = 2 }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public BoxContainer Rows { get; } = new VBoxContainer { Name = "RowHints" }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public BoxContainer Columns { get; } = new HBoxContainer { Name = "ColumnHints" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	private Dictionary<Vector2I, Button> Buttons { get; } = [];

	public override void _Ready()
	{
		Name = nameof(NonogramDisplay);
		this.Add(
				Background,
				Main.Add(Spacer, Hints.Columns, Hints.Rows, Tiles)
			)
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

		Background.CustomMinimumSize = Size;

		foreach (Vector2I position in (Vector2I.One * Config.Length).AsRange())
		{
			var button = new Button
			{
				Name = $"Button {position}",
				Text = EmptyText,
				CustomMinimumSize = TileSize
			}
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
				.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
			AddChild(Buttons[position] = button);
			button.Pressed += () => OnTilePressed(button, position);
		}
	}
	public virtual void OnTilePressed(Button button, Vector2I position)
	{
		button.Text = CurrentPenMode switch
		{
			PenMode.Block when button.Text is EmptyText or FillText => BlockText,
			PenMode.Block => EmptyText,
			PenMode.Fill when button.Text is EmptyText => FillText,
			PenMode.Fill when button.Text is FillText => EmptyText,
			_ => button.Text
		};
	}

	public sealed record TileHints
	{
		public sealed partial class Label : RichTextLabel
		{
			public Label(Dictionary<Position, RichTextLabel> labels, Position position)
			{
				Text = "0";
				FitContent = true;
				CustomMinimumSize = TileSize;
				labels[position] = this.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
			}
		}
		public readonly record struct Position(Side Side, int Index);
		public enum Side { Row, Column }

		public BoxContainer Rows { get; } = new VBoxContainer { Name = "RowHints" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public BoxContainer Columns { get; } = new HBoxContainer { Name = "ColumnHints" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

		public IConfig Config
		{
			set
			{
				foreach (int i in Range(0, count: value.Length))
				{
					Rows.Add(new Label(Labels, new(Side: Side.Row, Index: i)));
					Columns.Add(new Label(Labels, new(Side: Side.Column, Index: i)));
				}
			}
		}

		private Dictionary<Position, RichTextLabel> Labels { get; } = [];

		public void WriteToLabels(IData data)
		{
			foreach (var (position, hint) in data.Hints())
			{
				if (!Labels.TryGetValue(position, out RichTextLabel? label)) { continue; }
				string format = position.Side switch
				{
					Side.Column => "\n",
					Side.Row => "\t",
					_ => ""
				};
				label.Text = hint.Aggregate("", (current, i) => current + format + i);
			}
		}
	}

	public interface IColours { Color NonogramBackground { get; } }
	public interface IData { Dictionary<TileHints.Position, List<int>> Hints(); }
	public interface IConfig
	{
		int Margin { get; }
		int Length { get; }
		int Scale { get; }

		Vector2I Size => Vector2I.One * Length;
		Vector2I TilesSize => Size * Scale;
		Vector2I BackgroundSize => Size * (Scale + 5);
	}
}
