using Godot;

using static Godot.Control;

namespace RSG.UI.Nonogram;

using static Display;

public static class DisplayExtensions
{
	private const string BlockText = "X", FillText = "O", EmptyText = " ";

	public static string Text(this PenMode mode) => mode switch
	{
		PenMode.Block => BlockText,
		PenMode.Fill => FillText,
		_ => EmptyText
	};
	public static string FillButton(this PenMode mode, string current)
	{
		return mode switch
		{
			PenMode.Block => current is EmptyText or FillText ? BlockText : EmptyText,
			PenMode.Fill => current is EmptyText ? FillText : EmptyText,
			_ => current
		};
	}
}

public abstract class ToolBar
{
	public required Container Container { get; init; }
	public abstract void AddTools();
	public abstract void RemoveTools();
}

public sealed class LabelHints(Display display) : Dictionary<LabelHints.Position, RichTextLabel>
{
	public readonly record struct Position(Side Side, int Index)
	{
		public string AsFormat() => Side switch { Side.Column => "\n", Side.Row => "\t", _ => "" };
		public int GetIndex(Vector2I position) => Side switch { Side.Column => position.Y, Side.Row => position.X, _ => -1 };
		public string AsFormatAggregate(string current, int i) => current + AsFormat() + i;
		public bool HasIndex(Vector2I position) => GetIndex(position) == Index;
		public static IEnumerable<Position> FromIndex(int index) => [new(Side.Row, index), new(Side.Column, index)];
		public HorizontalAlignment AsHorizontalAlignment() => Side switch
		{
			Side.Row => HorizontalAlignment.Right,
			Side.Column => HorizontalAlignment.Center,
			_ => HorizontalAlignment.Fill
		};
		public VerticalAlignment AsVerticalAlignment() => Side switch
		{
			Side.Row => VerticalAlignment.Center,
			Side.Column => VerticalAlignment.Bottom,
			_ => VerticalAlignment.Fill
		};
	}
	public int Size
	{
		set
		{
			IEnumerable<Position> currentPositions = Range(0, count: value)
				.SelectMany(Position.FromIndex);
			IEnumerable<Position> excessPositions = Range(0, count: display.Tiles.Columns)
				.SelectMany(Position.FromIndex)
				.Except(currentPositions);

			foreach (Position position in currentPositions)
			{
				if (!TryGetValue(position, out RichTextLabel? label))
				{
					label = CreateAt(position);
				}
				label.Text = "0";
			}
			foreach (Position position in excessPositions)
			{
				if (!TryGetValue(position, out RichTextLabel? label)) { continue; }
				Remove(position);
				display.HintsParent(position).RemoveChild(label);
				label.QueueFree();
			}
		}
	}

	public RichTextLabel CreateAt(Position position)
	{
		RichTextLabel label = this[position] = new()
		{
			Text = "0",
			FitContent = true,
			CustomMinimumSize = new(10, 10),
			HorizontalAlignment = position.AsHorizontalAlignment(),
			VerticalAlignment = position.AsVerticalAlignment(),
		};
		display.HintsParent(position).AddChild(label);
		return label
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	}
}
public sealed class TileButtons(Display display) : Dictionary<Vector2I, Button>
{
	public IImmutableDictionary<Vector2I, bool> TileStates => this.ToImmutableDictionary(
		pair => pair.Key,
		pair => pair.Value.Text is FillText
	);
	public string Hints(LabelHints.Position position) => this
		.Where(pair => position.HasIndex(position: pair.Key))
		.Select(pair => pair.Value.Text is FillText ? 1 : 0)
		.ToList()
		.Condense()
		.Where(i => i > 0)
		.Aggregate(string.Empty, position.AsFormatAggregate);
	public Button CreateAt(Vector2I position)
	{
		Button button = this[position] = new Button
		{
			Name = $"Tile {position}",
			Text = EmptyText,
			CustomMinimumSize = new(10, 10)
		};
		display.Tiles.AddChild(button);
		button.Pressed += () => display.OnTilePressed(button, position);
		return button
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	}
	public int Size
	{
		set
		{
			IEnumerable<Vector2I> currentPositions = (Vector2I.One * value).AsRange();
			IEnumerable<Vector2I> excessPositions = (Vector2I.One * display.Tiles.Columns).AsRange()
				.Except(currentPositions);

			foreach (Vector2I position in currentPositions)
			{
				if (!TryGetValue(position, out Button? button))
				{
					button = CreateAt(position);
				}
				button.Text = EmptyText;
				display.UpdateHintsAt(position);
			}
			foreach (Vector2I position in excessPositions)
			{
				if (!TryGetValue(position, out Button? button)) { continue; }
				Remove(position);
				display.Tiles.RemoveChild(button);
				button.QueueFree();
			}
		}
	}
}
public abstract record Data
{
	public IImmutableDictionary<Vector2I, bool> TileStates => Tiles.ToImmutableDictionary();
	public Dictionary<Vector2I, bool> Tiles { private get; init; } = [];
	public virtual void Change(Vector2I position, bool clicked) => Tiles[position] = clicked;
	public void UpdateDisplay(Display display)
	{
		foreach (var (position, state) in TileStates)
		{
			display.UpdateTileState(position, state);
		}
	}
	public bool Matches(Display display)
	{
		return display.AllTiles(predicate: pair =>
			TileStates.TryGetValue(pair.Key, out var filled)
			&& filled == (pair.Value.Text == PenMode.Fill.Text())
		);
	}
}
public interface IColours { Color NonogramBackground { get; } }
public abstract partial class DataDisplay<T> : Display where T : Data, new()
{
	public T Puzzle
	{
		get => new() { Tiles = Buttons.TileStates.ToDictionary() };
		set
		{
			foreach ((Vector2I position, Button button) in Buttons)
			{
				if (!value.TileStates.TryGetValue(position, out bool state)) { continue; }
				button.Text = state ? FillText : EmptyText;
				UpdateHintsAt(position);
			}
		}
	}
}
public abstract partial class Display : Container
{
	public const string BlockText = "X", FillText = "O", EmptyText = " ", EmptyHint = "0";
	public enum PenMode { Block, Fill, Clear }
	public enum Side { Row, Column }
	public PenMode Pen { get; set; } = PenMode.Fill;
	public int PuzzleSize
	{
		get => Tiles.Columns; set => Tiles.Columns = Labels.Size = Buttons.Size = value;
	}
	public GridContainer Tiles { get; } = new GridContainer { Name = "Tiles", Columns = 2 }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public Control Spacer { get; } = new Control { Name = "Spacer" }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public GridContainer Main { get; } = new GridContainer { Name = "MainContainer", Columns = 2 }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public VBoxContainer Rows { get; } = new VBoxContainer { Name = "RowHints" }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.CenterRight, resizeMode: LayoutPresetMode.KeepSize);
	public HBoxContainer Columns { get; } = new HBoxContainer { Name = "ColumnHints" }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.CenterBottom, resizeMode: LayoutPresetMode.KeepSize);
	protected TileButtons Buttons { get; }
	protected LabelHints Labels { get; }
	public Display()
	{
		Buttons = new(this);
		Labels = new(this);
	}
	public virtual void Reset() => _ = Buttons.Values.Select(button => button.Text = EmptyText);
	public override void _Ready()
	{
		this.Add(Main.Add(Spacer, Columns, Rows, Tiles))
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	}
	public bool AllTiles(Func<KeyValuePair<Vector2I, Button>, bool> predicate) => Buttons.All(predicate);
	public void UpdateTileState(Vector2I position, bool state)
	{
		if (!Buttons.TryGetValue(position, out var button)) return;
		button.Text = state ? FillText : EmptyText;
		UpdateHintsAt(position);
	}
	public virtual void OnTilePressed(Button button, Vector2I position) => button.Text = Pen.FillButton(button.Text);
	public void UpdateHintsAt(params Span<LabelHints.Position> hints)
	{
		foreach (var hint in hints) UpdateHintsAt(position: hint);
	}
	public void UpdateHintsAt(OneOf<LabelHints.Position, Vector2I> position) => position.Switch(
		hint =>
		{
			if (!Labels.TryGetValue(hint, out var label)) return;
			label.Text = Buttons.Hints(hint);
		},
		vector => UpdateHintsAt(new(Side.Row, vector.X), new(Side.Column, vector.Y))
	);
	public Node HintsParent(LabelHints.Position position)
	{
		return position.Side switch { Side.Row => Rows, Side.Column => Columns, _ => this };
	}
}