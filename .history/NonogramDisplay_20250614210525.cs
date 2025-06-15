using Godot;

namespace RSG.UI;

public abstract partial class NonogramDisplay : Container
{
	public const string BlockText = "X", FillText = "O", EmptyText = " ";
	public enum PenMode { Block, Fill }
	private static Vector2 TileSize = new(40, 40);

	public required IConfig Config
	{
		get; init
		{
			Background.UniformPadding(margin: value.Margin);
			Hints.PlaceLabels(config: value);
			Tiles.CustomMinimumSize = value.TilesSize;
			Tiles.FreeAll();
			Tiles.PlaceButtons(length: value.Length, pressed: OnTilePressed);
			Tiles.Columns = value.Length;
			field = value;
		}
	}
	public IColours Colours { set => Background.Color = new(0, 0, 0); }
	public PenMode CurrentPenMode { get; set; } = PenMode.Fill;
	public TileHints Hints => field ??= new();
	public TilesContainer Tiles => field ??= new(length: Config.Length);
	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = new(0, 0, 0) }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public Control Spacer { get; } = new Control { Name = "Spacer" }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public GridContainer Main { get; } = new GridContainer { Name = "MainContainer", Columns = 2 }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

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

	public sealed partial class TilesContainer : GridContainer
	{
		private Dictionary<Vector2I, Button> Buttons { get; } = [];
		public TilesContainer(int length)
		{
			Name = "Tiles";
			Columns = length;
			this.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
				.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		}
		public void FreeAll() => Buttons.FreeAll(this);
		public void PlaceButtons(int length, Action<Button, Vector2I> pressed)
		{
			foreach (Vector2I position in (Vector2I.One * length).AsRange())
			{
				AddChild(Buttons[position] = new TileButton(position, pressed));
			}
		}
	}
	public sealed partial class TileButton : Button
	{
		public TileButton(Vector2I position, Action<Button, Vector2I> pressed)
		{
			Name = $"Button {position}";
			Text = EmptyText;
			CustomMinimumSize = TileSize;
			Pressed += () => pressed(this, position);

			this.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
				.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		}
	}
	public sealed partial class HintLabel : RichTextLabel
	{
		public HintLabel(Dictionary<TileHints.Position, RichTextLabel> labels, TileHints.Position position)
		{
			Text = "0";
			FitContent = true;
			CustomMinimumSize = TileSize;
			this.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
			labels[position] = this;
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
		public void PlaceLabels(IConfig config)
		{
			foreach (int i in Range(0, count: config.Length))
			{
				Rows.Add(new HintLabel(Labels, new(Side: Side.Row, Index: i)));
				Columns.Add(new HintLabel(Labels, new(Side: Side.Column, Index: i)));
			}
		}
		public void WriteToLabels(IData data)
		{
			foreach (var (position, hint) in data.Hints())
			{
				string format = position.Side switch
				{
					Side.Column => "\n",
					Side.Row => "\t",
					_ => ""
				};

				if (Labels.TryGetValue(position, out RichTextLabel? label))
				{
					label.Text = hint.Aggregate("", (current, i) => current + format + i);
				}
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
