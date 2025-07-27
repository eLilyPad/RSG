using Godot;
using static Godot.Control;

namespace RSG.UI.Nonogram;

using static Display;
using static NonogramContainer;

public static class DisplayExtensions
{
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
	public static string CalculateHints(this Dictionary<Vector2I, Button> buttons, LabelPosition position)
	{
		return buttons
			.Where(pair => position.HasIndex(position: pair.Key))
			.Select(pair => pair.Value.Text is FillText ? 1 : 0)
			.ToList()
			.Condense()
			.Where(i => i > 0)
			.Aggregate(EmptyHint, (current, i) => i > 0 && current == EmptyHint
				? position.AsFormat() + i
				: current + position.AsFormat() + i
			);
	}
}
public interface IColours { Color NonogramBackground { get; } }
public abstract class ToolBar
{
	public required Container Container { get; init; }
	public abstract void AddTools();
	public abstract void RemoveTools();

	public sealed class Paint : ToolBar
	{
		public Button SaveAs { get; } = new() { Name = "Save", Text = "Save As" };
		public Button SaveAsCode { get; } = new() { Name = "SaveCode", Text = "As Code" };
		public LineEdit NameInput { get; } = new LineEdit { Name = "NameInput", TooltipText = "New Puzzle" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public override void AddTools() => Container.Add(SaveAs, NameInput, SaveAsCode);
		public override void RemoveTools() => Container.Remove(SaveAs, NameInput, SaveAsCode);
	}
	public sealed class Game : ToolBar
	{
		public Button CheckProgress { get; } = new() { Name = "CheckProgress", Text = "Check" };
		public RichTextLabel ProgressReport { get; } = new RichTextLabel
		{
			Name = "ProgressReport",
			SizeFlagsStretchRatio = 0.05f
		}
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
		public override void AddTools() => Container.Add(CheckProgress, ProgressReport);
		public override void RemoveTools() => Container.Remove(CheckProgress, ProgressReport);
	}
}
public abstract record Data
{
	public int Size => Mathf.RoundToInt(Mathf.Sqrt(Tiles.Count));
	public IImmutableDictionary<Vector2I, bool> TileStates => Tiles.ToImmutableDictionary();
	public Dictionary<Vector2I, bool> Tiles { private get; init; } = [];
	public virtual void Change(Vector2I position, bool clicked) => Tiles[position] = clicked;
	public void UpdateDisplay(Display display)
	{
		foreach (var (position, state) in TileStates)
		{
			display.UpdateTileState(position, state);
			display.UpdateHintsAt(position);
		}
	}
	public bool Matches(Display display) => display.AllTiles(predicate: pair =>
		TileStates.TryGetValue(pair.Key, out var filled)
		&& filled == (pair.Value.Text == PenMode.Fill.Text())
	);
	public void Reset()
	{
		foreach (var (position, _) in TileStates) { Change(position, clicked: false); }
	}
}
public abstract partial class PuzzleDisplay : Display
{
	public readonly record struct Empty(int Size);
	public OneOf<PuzzleData, string, Empty> Puzzle
	{
		get => new PuzzleData
		{
			Tiles = Buttons.ToDictionary(
				keySelector: pair => pair.Key,
				elementSelector: pair => pair.Value.Text is FillText
			)
		};
		set => value.Switch(
			data => data.UpdateDisplay(this),
			value => CodeSaver.Decode(value).UpdateDisplay(this),
			empty => new PuzzleData(empty.Size).UpdateDisplay(this)
		);
	}
}
public abstract partial class Display : Container
{
	public readonly record struct LabelPosition(Side Side, int Index)
	{
		public bool HasIndex(Vector2I position) => GetIndex(position) == Index;
		public static IEnumerable<LabelPosition> FromIndex(int Index) => [new(Side.Row, Index), new(Side.Column, Index)];
		public string AsFormat() => Side switch { Side.Column => "\n", Side.Row => "\t", _ => "" };
		public int GetIndex(Vector2I position) => Side switch { Side.Column => position.Y, Side.Row => position.X, _ => -1 };
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

	public const string BlockText = "X", FillText = "O", EmptyText = " ", EmptyHint = "0";
	public enum PenMode { Block, Fill, Clear }
	public enum Side { Row, Column }
	public PenMode Pen { get; set; } = PenMode.Fill;
	public int PuzzleSize
	{
		get => Tiles.Columns;
		set
		{
			Labels.Refill(
				values: Range(0, count: value).SelectMany(LabelPosition.FromIndex),
				create: CreateHint,
				parent: HintsParent,
				reset: position => Labels[position].Text = EmptyHint
			);
			Buttons.Refill(
				values: (Vector2I.One * value).GridRange(),
				create: CreateTile,
				parent: _ => Tiles,
				reset: position => Buttons[position].Text = EmptyText
			);
			Tiles.Columns = value;
		}
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
	protected Dictionary<Vector2I, Button> Buttons { get; } = [];
	protected Dictionary<LabelPosition, RichTextLabel> Labels { get; } = [];
	public virtual void OnTilePressed(Button button, Vector2I position) => button.Text = Pen.FillButton(button.Text);
	public virtual void Reset()
	{
		_ = Buttons.Values.Select(button => button.Text = EmptyText);
		_ = Labels.Values.Select(label => label.Text = EmptyHint);
	}
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
	public void UpdateAllHints()
	{
		foreach (var position in Labels.Keys) { UpdateHintsAt(position); }
	}
	public void UpdateHintsAt(params Span<LabelPosition> hints)
	{
		foreach (var hint in hints) UpdateHintsAt(position: hint);
	}
	public void UpdateHintsAt(OneOf<LabelPosition, Vector2I> position) => position.Switch(
		hint =>
		{
			if (!Labels.TryGetValue(hint, out var label)) return;
			label.Text = Buttons.CalculateHints(hint);
		},
		vector => UpdateHintsAt(new(Side.Row, vector.X), new(Side.Column, vector.Y))
	);
	public Node HintsParent(LabelPosition position)
	{
		return position.Side switch { Side.Row => Rows, Side.Column => Columns, _ => this };
	}

	private Button CreateTile(Vector2I position)
	{
		var button = Buttons[position] = new Button
		{
			Name = $"Tile {position}",
			Text = EmptyText,
			CustomMinimumSize = new(10, 10)
		}
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
		button.Pressed += () => OnTilePressed(button, position);
		return button;
	}
	private RichTextLabel CreateHint(LabelPosition position)
	{
		return Labels[position] = new RichTextLabel
		{
			Text = EmptyHint,
			FitContent = true,
			CustomMinimumSize = new(10, 10),
			HorizontalAlignment = position.AsHorizontalAlignment(),
			VerticalAlignment = position.AsVerticalAlignment(),
		}
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	}
}