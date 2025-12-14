using System.Text.Json;
using Godot;

namespace RSG.Nonogram;

public abstract partial class Display : AspectRatioContainer
{
	public readonly record struct TilePosition(Vector2I Position)
	{
		public static implicit operator TilePosition(Vector2I position) => new(position);
		public static implicit operator Vector2I(TilePosition tile) => tile.Position;
	}
	public readonly record struct HintPosition(Side Side, int Index)
	{
		public static IEnumerable<HintPosition> AsRange(int length, int start = 0) => Range(start, count: length)
			.SelectMany(i => Convert(i));
		public static IEnumerable<HintPosition> Convert(OneOf<Vector2I, int> value) => [
			new(Side.Row, value.Match(position => position.X, index => index)),
			new(Side.Column, value.Match(position => position.Y, index => index))
		];

		public readonly string AsFormat()
		{
			return Side switch
			{
				Side.Column => "\n",
				Side.Row => "\t",
				_ => ""
			};
		}
		public readonly int IndexFrom(Vector2I position)
		{
			return Side switch
			{
				Side.Column => position.Y,
				Side.Row => position.X,
				_ => -1
			};
		}
		public readonly (HorizontalAlignment, VerticalAlignment) Alignment() => (
			Side switch
			{
				Side.Row => HorizontalAlignment.Right,
				Side.Column => HorizontalAlignment.Center,
				_ => HorizontalAlignment.Fill
			},
			Side switch
			{
				Side.Row => VerticalAlignment.Center,
				Side.Column => VerticalAlignment.Bottom,
				_ => VerticalAlignment.Fill
			}
		);
	}
	public abstract record Data
	{
		public readonly record struct Empty(int Size);
		public static class PropertyNames
		{
			public const string Tiles = "Tiles", Name = "Name", Position = "Position", Value = "Value";
		}
		public static string ReadName(JsonElement root)
		{
			if (!root.TryGetProperty(PropertyNames.Name, out JsonElement nameProp))
			{
				return DefaultName;
			}
			return nameProp.GetString() ?? DefaultName;
		}
		public static Dictionary<Vector2I, TileMode> ReadTiles(JsonElement tilesProp)
		{
			Dictionary<Vector2I, TileMode> tiles = [];
			foreach (JsonElement element in tilesProp.EnumerateArray())
			{
				if (!element.TryGetProperty(PropertyNames.Position, out JsonElement positionProp)
					|| !positionProp.GetString().TryParse(out Vector2I position)
				)
				{
					GD.PrintErr($"Error parsing position in JSON: {element}");
					continue;
				}
				if (!element.TryGetProperty(PropertyNames.Value, out JsonElement valueProp))
				{
					GD.PrintErr($"Error parsing value in JSON: {element}");
					continue;
				}
				//tiles[position] = valueProp.GetInt32().ToTileMode();
				tiles[position] = valueProp.TryGetInt32(out int value) ? value.ToTileMode() : TileMode.Clear;
			}
			return tiles;
		}
		public static IImmutableDictionary<Vector2I, TileMode> AsStates(Display display) => display.Tiles.ToImmutableDictionary(
			keySelector: pair => pair.Key,
			elementSelector: pair => pair.Value.Button.Text is FillText ? TileMode.Fill : TileMode.Clear
		);

		public const string DefaultName = "Puzzle";
		public const int DefaultSize = 15;
		public virtual string Name { get; set; } = DefaultName;
		public IImmutableDictionary<Vector2I, TileMode> States => Tiles.ToImmutableDictionary();
		public IEnumerable<HintPosition> HintPositions => Tiles.Keys.SelectMany(
			key => HintPosition.Convert(key)
		);
		public Dictionary<Vector2I, TileMode> Tiles { protected get; init; } = (Vector2I.One * DefaultSize)
			.GridRange()
			.ToDictionary(elementSelector: _ => TileMode.Clear);
		public virtual int Size => (int)Mathf.Sqrt(Tiles.Count);
		public Data(int size = DefaultSize)
		{
			Tiles = (Vector2I.One * size).GridRange().ToDictionary(elementSelector: _ => TileMode.Clear);
		}
		public Data(Display display)
		{
			Tiles = display.Tiles.ToDictionary(elementSelector: selector);
			static TileMode selector(KeyValuePair<Vector2I, Tile> pair) => pair.Value.Button.Text.FromText();
		}

		public bool Matches(Display display, Vector2I position)
		{
			if (!States.TryGetValue(position, out TileMode state)
				|| !display.Tiles.TryGetValue(position, out Tile? tile)
				|| !tile.Button.Matches(state)
			) return false;
			return true;
		}
		public bool Matches(Data expected)
		{
			foreach ((Vector2I position, TileMode state) in States)
			{
				if (!expected.Tiles.TryGetValue(position, out TileMode tile)) return false;
				if (tile is not TileMode.Fill) continue;
				if (tile != state) return false;
			}
			return true;
		}
		public virtual bool Matches(Display display)
		{
			foreach ((Vector2I position, TileMode state) in States)
			{
				if (!display.Tiles.TryGetValue(position, out Tile? tile)
					|| !tile.Button.Matches(state)
				) return false;
			}
			return true;
		}
	}
	private sealed partial class DefaultDisplay : Display
	{
		public override void Reset() { }
	}

	public const string BlockText = "X", FillText = "O", EmptyText = " ", EmptyHint = "0";
	public const int TileSize = 31;
	// [Bug] ^^ if this changes the tile becomes a rectangle, with the height being larger than the width
	public const MouseButton FillButton = MouseButton.Left, BlockButton = MouseButton.Right;
	public enum TileMode : int { Block = 2, Fill = 1, Clear = 0 }
	public enum Side { Row, Column }

	public static Display Default { get; } = new DefaultDisplay { Name = "Puzzle Display" };

	protected GridContainer TilesGrid { get; } = new GridContainer { Name = "Tiles", Columns = 2 }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	protected Control Spacer { get; } = new Control { Name = "Spacer" }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	protected GridContainer Grid { get; } = new GridContainer { Name = "MainContainer", Columns = 2 }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	protected VBoxContainer Rows { get; } = new VBoxContainer { Name = "RowHints" }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.Expand)
	.Preset(preset: LayoutPreset.CenterRight, resizeMode: LayoutPresetMode.KeepSize);
	protected HBoxContainer Columns { get; } = new HBoxContainer { Name = "ColumnHints" }
	.SizeFlags(horizontal: SizeFlags.Expand, vertical: SizeFlags.ExpandFill)
	.Preset(preset: LayoutPreset.CenterBottom, resizeMode: LayoutPresetMode.KeepSize);
	protected MarginContainer Margin { get; } = new MarginContainer { Name = "Margin" }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize, 20);

	protected Dictionary<Vector2I, Tile> Tiles { get; } = [];
	protected Dictionary<HintPosition, Hint> Hints { get; } = [];

	private Func<HintPosition, Node> HintsParent => pos => pos.Side switch { Side.Row => Rows, Side.Column => Columns, _ => this };

	public override sealed void _Ready()
	{
		this.Add(Margin.Add(Grid.Add(Spacer, Columns, Rows, TilesGrid)))
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	}
	public abstract void Reset();
	public virtual void Load(Data data)
	{
		ChangePuzzleSize(data.Size);
		WriteToTiles(data switch { SaveData save => save.Expected, _ => data });
		WriteToHints(data.HintPositions);
	}
	public virtual void OnTilePressed(Vector2I position)
	{
		if (!Tiles.TryGetValue(position, out Tile? button)) return;
		bool
		blocked = Input.IsMouseButtonPressed(BlockButton),
		filled = Input.IsMouseButtonPressed(FillButton);
		//if (!blocked && !filled) return;
		string text = button.Button.Text;
		TileMode previousMode = button.Button.Text.FromText();
		TileMode input = blocked ? TileMode.Block
			: filled ? TileMode.Fill
			: TileMode.Clear;

		button.Button.Text = (previousMode, input) switch
		{
			_ when previousMode == input => EmptyText,
			(_, TileMode.Fill) => FillText,
			(_, TileMode.Block) => BlockText,
			(_, TileMode.Clear) => EmptyText,
			_ => text
		};
	}
	public string CalculateHintAt(HintPosition position) => Tiles.CalculateHints(position);
	public void WriteToHints(IEnumerable<HintPosition> positions)
	{
		foreach (HintPosition position in positions)
		{
			if (!Hints.TryGetValue(position, out Hint? hint)) { continue; }
			hint.Text = CalculateHintAt(position);
		}
	}
	public void WriteToTiles(Data data)
	{
		foreach ((Vector2I position, Tile tile) in Tiles)
		{
			tile.Button.Text = data switch
			{
				SaveData save => save.States.AsText(position),
				_ => data.States.AsText(position)
			};
		}
	}
	public void ChangePuzzleSize(int size)
	{
		if (size < 1)
		{
			GD.PrintErr($"size: ({size}) < 1: replacing with default size {Data.DefaultSize}");
			size = Data.DefaultSize;
		}
		IEnumerable<HintPosition> values = HintPosition.AsRange(TilesGrid.Columns = size);
		Hints.Refill(values, create: CreateHint, parent: HintsParent, reset: (Action<Hint>)ResetHint);
		Tiles.RefillGrid(size, create: CreateTile, parent: TilesGrid, reset: (Action<Tile>)ResetTile);

		Hint CreateHint(HintPosition position) => new(position);
		Tile CreateTile(Vector2I position)
		{
			Tile button = new() { Name = $"Tile (X: {position.X}, Y: {position.Y})" };
			button.Button.MouseEntered += MouseEntered;
			button.Button.ButtonDown += ButtonDown;

			return button;

			void ButtonDown()
			{
				OnTilePressed(position);
			}
			void MouseEntered()
			{
				bool fill = Input.IsMouseButtonPressed(FillButton);
				bool block = Input.IsMouseButtonPressed(BlockButton);
				if (!fill && !block) { return; }
				OnTilePressed(position);
			}
		}
	}
	protected virtual void ResetTile(Tile button) => button.Button.Text = EmptyText;
	protected virtual void ResetHint(Hint hint) => hint.Text = EmptyHint;
}
public sealed partial class Hint : RichTextLabel
{
	public Hint(Display.HintPosition position)
	{
		Name = $"Hint (Side: {position.Side}, Index: {position.Index})";
		Text = Display.EmptyHint;
		FitContent = true;
		CustomMinimumSize = Vector2.One * Display.TileSize;
		(HorizontalAlignment, VerticalAlignment) = position.Alignment();
	}
}
public sealed partial class Tile : AspectRatioContainer
{
	public Button Button { get; } = new()
	{
		Text = Display.EmptyText,
		CustomMinimumSize = Vector2.One * Display.TileSize,
		ButtonMask = MouseButtonMask.Left | MouseButtonMask.Right
	};
	public override void _Ready() => this.Add(Button);
}