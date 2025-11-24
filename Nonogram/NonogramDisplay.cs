using System.Text.Json;
using Godot;

namespace RSG.Nonogram;

public abstract partial class Display : Container
{
	public readonly record struct TilePosition(Vector2I Position)
	{
		public static implicit operator TilePosition(Vector2I position) => new(position);
		public static implicit operator Vector2I(TilePosition tile) => tile.Position;
	}
	public readonly record struct HintPosition(Side Side, int Index)
	{
		public static implicit operator HorizontalAlignment(HintPosition position) => position.Side switch
		{
			Side.Row => HorizontalAlignment.Right,
			Side.Column => HorizontalAlignment.Center,
			_ => HorizontalAlignment.Fill
		};
		public static implicit operator VerticalAlignment(HintPosition position) => position.Side switch
		{
			Side.Row => VerticalAlignment.Center,
			Side.Column => VerticalAlignment.Bottom,
			_ => VerticalAlignment.Fill
		};
		public static IEnumerable<HintPosition> AsRange(int length, int start = 0) => Range(start, count: length)
			.SelectMany(i => Convert(i));
		public static IEnumerable<HintPosition> Convert(OneOf<Vector2I, int> value) => [
			new(Side.Row, value.Match(position => position.X, index => index)),
			new(Side.Column, value.Match(position => position.Y, index => index))
		];
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
		public static Dictionary<Vector2I, bool> ReadTiles(JsonElement tilesProp)
		{
			Dictionary<Vector2I, bool> tiles = [];
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
				tiles[position] = valueProp.GetBoolean();
			}
			return tiles;
		}
		public static IImmutableDictionary<Vector2I, bool> AsStates(Display display) => display.Tiles.ToImmutableDictionary(
			keySelector: pair => pair.Key,
			elementSelector: pair => pair.Value.Text is FillText
		);
		private static bool Matches(Button tile, bool state)
		{
			return (tile.Text is FillText && state) || (tile.Text is EmptyText && !state);
		}
		public const string DefaultName = "Puzzle";
		public const int DefaultSize = 15;
		public string Name { get; set; } = DefaultName;
		public IImmutableDictionary<Vector2I, bool> States => Tiles.ToImmutableDictionary();
		public IEnumerable<HintPosition> HintPositions => Tiles.Keys.SelectMany(
			key => HintPosition.Convert(key)
		);
		public Dictionary<Vector2I, bool> Tiles { protected get; init; } = (Vector2I.One * DefaultSize)
			.GridRange()
			.ToDictionary(elementSelector: _ => false);
		public virtual int Size => (int)Mathf.Sqrt(Tiles.Count);
		public Data(int size = DefaultSize)
		{
			Tiles = (Vector2I.One * size).GridRange().ToDictionary(elementSelector: _ => false);
		}
		public Data(Display display)
		{
			Tiles = display.Tiles.ToDictionary(elementSelector: pair => pair.Value.Text is FillText);
		}
		public string StateAsText(Vector2I position) => States.GetValueOrDefault(position) ? FillText : EmptyText;
		public bool Matches(Display display, Vector2I position)
		{
			if (!States.TryGetValue(position, out bool state)
				|| !display.Tiles.TryGetValue(position, out Button? tile)
				|| !Matches(tile, state)
			) return false;
			return true;
		}
		public bool Matches(Display display)
		{
			foreach ((Vector2I position, bool state) in States)
			{
				if (!display.Tiles.TryGetValue(position, out Button? tile)
					|| !Matches(tile, state)
				) return false;
			}
			return true;
		}
	}

	public const string BlockText = "X", FillText = "O", EmptyText = " ", EmptyHint = "0";
	public const int TileSize = 31;
	// [Bug] ^^ if this changes the tile becomes a rectangle, with the height being larger than the width
	public enum PenMode { Block, Fill, Clear }
	public enum Side { Row, Column }
	public PenMode Pen { get; set; } = PenMode.Fill;

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

	protected Dictionary<Vector2I, Button> Tiles { get; } = [];
	protected Dictionary<HintPosition, RichTextLabel> Hints { get; } = [];

	private Func<HintPosition, Node> HintsParent => pos => pos.Side switch { Side.Row => Rows, Side.Column => Columns, _ => this };

	public override void _Ready()
	{
		this.Add(Margin.Add(Grid.Add(Spacer, Columns, Rows, TilesGrid)))
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	}
	public abstract void Reset();
	public abstract void Load(Data data);
	public virtual void OnTilePressed(Vector2I position)
	{
		Button button = Tiles[position];
		button.Text = Pen.FillButton(button.Text);
	}
	public string CalculateHintAt(HintPosition position) => Tiles.CalculateHints(position);
	public void ChangePuzzleSize(int size)
	{
		if (size < 1)
		{
			GD.PrintErr($"size: ({size}) < 1: replacing with default size {Data.DefaultSize}");
			size = Data.DefaultSize;
		}
		IEnumerable<HintPosition> values = HintPosition.AsRange(TilesGrid.Columns = size);
		Hints.Refill(values, create: CreateHint, parent: HintsParent, reset: (Action<RichTextLabel>)ResetHint);
		Tiles.RefillGrid(size, create: CreateTile, parent: TilesGrid, reset: (Action<Button>)ResetTile);
	}
	protected virtual void ResetTile(Button button) => button.Text = EmptyText;
	protected virtual void ResetHint(RichTextLabel hint) => hint.Text = EmptyHint;

	private Button CreateTile(Vector2I position)
	{
		const MouseButton pressedMouseButton = MouseButton.Left;
		Button button = new()
		{
			Name = $"Tile (X: {position.X}, Y: {position.Y})",
			Text = EmptyText,
			CustomMinimumSize = Vector2.One * TileSize
		};
		button.MouseEntered += MouseEntered;
		button.ButtonDown += ButtonDown;

		return button;

		void ButtonDown() => OnTilePressed(position);
		void MouseEntered()
		{
			bool pressed = Input.IsMouseButtonPressed(pressedMouseButton);
			if (!pressed) { return; }
			OnTilePressed(position);
		}
	}
	private RichTextLabel CreateHint(HintPosition position)
	{
		return new RichTextLabel
		{
			Text = EmptyHint,
			FitContent = true,
			CustomMinimumSize = Vector2.One * TileSize,
			HorizontalAlignment = position,
			VerticalAlignment = position,
		};
	}
}