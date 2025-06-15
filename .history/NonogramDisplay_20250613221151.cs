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
			Hints.PlaceLabels(length: value);
			Tiles.PlaceButtons(length: value, pressed: OnTilePressed);
			Tiles.Columns = field = value;
		}
	} = 5;

	public TileHints Hints => field ??= new();
	public TilesContainer Tiles => field ??= new(length: TilesLength);
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

	public abstract void OnTilePressed(Button button, Vector2I position);
	public virtual void UpdateSettings(IColours colours, IConfig config)
	{
		Tiles.CustomMinimumSize = config.TilesSize;
		Background
			.UniformPadding(IConfig.Margin)
			.Color = new(0, 0, 0);
	}

	public sealed partial class TilesContainer : GridContainer
	{
		public IEnumerable<Vector2I> ButtonPositions => Buttons.Keys;
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
		private RichTextLabel CreateHint(Position position)
		{
			var label = new RichTextLabel { Text = "0", FitContent = true, CustomMinimumSize = TileSize }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
			Labels[position] = label;
			return label;
		}
	}

	public interface IColours { Color NonogramBackground { get; } }
	public interface IData { Dictionary<TileHints.Position, List<int>> Hints(); }
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
