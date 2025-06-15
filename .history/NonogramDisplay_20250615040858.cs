using Godot;

namespace RSG.UI;

public abstract partial class NonogramDisplay : Container
{
	public readonly record struct HintPosition(Side Side, int Index)
	{
		public static (HintPosition Row, HintPosition Column) ToPosition(Vector2I position) => (
			new(Side.Row, position.X),
			new(Side.Column, position.Y)
		);
	}
	public const string BlockText = "X", FillText = "O", EmptyText = " ";
	public enum PenMode { Block, Fill }
	private static Vector2 TileSize = new(40, 40);
	public enum Side { Row, Column }


	public Dictionary<Vector2I, bool> ButtonStates => Buttons.ToDictionary(
		keySelector: pair => pair.Key,
		elementSelector: pair => pair.Value.Text is FillText
	);
	public abstract IConfig Config { get; }
	public IColours Colours { set => Background.Color = new(0, 0, 0); }
	public PenMode CurrentPenMode { get; set; } = PenMode.Fill;

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

	protected Dictionary<Vector2I, Button> Buttons { get; } = [];
	protected Dictionary<HintPosition, RichTextLabel> Labels { get; } = [];

	public override void _Ready()
	{
		this
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		Background.CustomMinimumSize = Size;
		AddChildren();
		foreach (Vector2I position in (Vector2I.One * Config.Length).AsRange())
		{
			var button = Buttons[position] = new Button
			{
				Name = $"Button {position}",
				Text = EmptyText,
				CustomMinimumSize = TileSize
			}
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
			Tiles.Add(button);
			button.Pressed += () => OnTilePressed(button, position);
		}
		foreach (int i in Range(0, count: Config.Length))
		{
			Rows.Add(Label(Side: Side.Row));
			Columns.Add(Label(Side: Side.Column));

			RichTextLabel Label(Side Side) => Labels[new(Side, Index: i)] = new RichTextLabel
			{
				Text = "0",
				FitContent = true,
				CustomMinimumSize = TileSize
			}
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
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
	protected virtual void AddChildren()
	{
		this.Add(
			Background,
			Main.Add(Spacer, Columns, Rows, Tiles)
		);
	}

	public interface IColours { Color NonogramBackground { get; } }
	public interface IData
	{
		void Change(Vector2I position, bool clicked);
		Dictionary<HintPosition, List<int>> Hints();
	}
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
