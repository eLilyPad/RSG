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

		public readonly string AsFormat() => Side switch
		{
			Side.Column => "\n",
			Side.Row => "\t",
			_ => ""
		};
		public readonly int IndexFrom(Vector2I position) => Side switch
		{
			Side.Column => position.Y,
			Side.Row => position.X,
			_ => -1
		};
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
	public sealed partial class Default : Display
	{
		public override void Reset() { }
	}

	public const string BlockText = "X", FillText = "O", EmptyText = " ", EmptyHint = "0";
	public const int TileSize = 0;
	// [Bug] ^^ if this changes the tile becomes a rectangle, with the height being larger than the width
	public const MouseButton FillButton = MouseButton.Left, BlockButton = MouseButton.Right;
	public enum TileMode : int { Block = 2, Fill = 1, Clear = 0 }
	public enum Side { Row, Column }

	public static TileMode PressedMode => Input.IsMouseButtonPressed(BlockButton) ? TileMode.Block
		: Input.IsMouseButtonPressed(FillButton) ? TileMode.Fill
		: TileMode.Clear;

	public IColours Colours { protected get; set; } = ColourPack.Default;

	public MarginContainer Margin { get; } = new MarginContainer { }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public GridContainer TilesGrid { get; } = new GridContainer { Name = "Tiles", Columns = 2 }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public Container Spacer { get; } = new AspectRatioContainer { Name = "Spacer" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public GridContainer Grid { get; } = new GridContainer { Name = "MainContainer", Columns = 2 }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public VBoxContainer Rows { get; } = new VBoxContainer { Name = "RowHints" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public HBoxContainer Columns { get; } = new HBoxContainer { Name = "ColumnHints" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);

	protected Dictionary<Vector2I, Tile> Tiles { get; } = [];
	protected Dictionary<HintPosition, Hint> Hints { get; } = [];

	public override sealed void _Ready() => this.Add(
		Margin.Add(Grid.Add(Spacer, Columns, Rows, TilesGrid))
	);
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
		TileMode input = PressedMode;
		TileMode previousMode = button.Button.Text.FromText();
		button.Button.Text = input == previousMode ? EmptyText : input.AsText();
	}
	public string CalculateHintAt(HintPosition position) => Tiles.CalculateHints(position);
	public void WriteToHints(IEnumerable<HintPosition> positions)
	{
		foreach (HintPosition position in positions)
		{
			if (!Hints.TryGetValue(position, out Hint? hint)) { continue; }
			hint.Label.Text = CalculateHintAt(position);
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
		IEnumerable<HintPosition> hintValues = HintPosition.AsRange(TilesGrid.Columns = size);
		IEnumerable<Vector2I> tileValues = (Vector2I.One * size).GridRange();

		foreach (HintPosition position in hintValues)
		{
			if (!Hints.TryGetValue(position, out Hint? node))
			{
				node = Hints[position] = Hint.Create(position: position, Colours);
				HintsParent(position).AddChild(node);
			}
			ResetHint(node);
		}
		foreach (Vector2I position in tileValues)
		{
			if (!Tiles.TryGetValue(position, out Tile? node))
			{
				node = Tiles[position] = Tile.Create(position, Colours, pressed: OnTilePressed);
				TilesGrid.AddChild(node);
			}
			ResetTile(node);
		}

		foreach (HintPosition position in Hints.Keys.Except(hintValues))
		{
			if (!Hints.TryGetValue(position, out Hint? node)) { continue; }
			HintsParent(position).Remove(free: true, node);
			Hints.Remove(position);
		}
		foreach (Vector2I position in Tiles.Keys.Except(tileValues))
		{
			if (!Tiles.TryGetValue(position, out Tile? node)) { continue; }
			Tiles.Remove(position);
			TilesGrid.Remove(free: true, node);
		}

		Node HintsParent(HintPosition position) => position.Side switch
		{
			Side.Row => Rows,
			Side.Column => Columns,
			_ => this
		};
	}
	protected virtual void ResetTile(Tile button) => button.Button.Text = EmptyText;
	protected virtual void ResetHint(Hint hint) => hint.Label.Text = EmptyHint;
}
public sealed partial class Hint : Control
{
	public static Hint Create(Display.HintPosition position, IColours colours)
	{
		Hint hint = new Hint
		{
			Name = $"Hint (Side: {position.Side}, Index: {position.Index})",
			CustomMinimumSize = Vector2.One * Display.TileSize,
			Label = new RichTextLabel
			{
				Name = "Label",
				Text = Display.EmptyHint,
				FitContent = true,
			}.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill)
		}.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		(hint.Label.HorizontalAlignment, hint.Label.VerticalAlignment) = position.Alignment();
		hint.Label.AddThemeFontSizeOverride("normal_font_size", 10);
		hint.Background.Color = position.Index % 2 == 0 ? colours.NonogramHintBackground1 : colours.NonogramHintBackground2;
		return hint;
	}
	public required RichTextLabel Label { get; init; }
	public ColorRect Background { get; } = new ColorRect { Name = "Background" }
		.Preset(LayoutPreset.FullRect);
	private Hint() { }
	public override void _Ready() => this.Add(Background, Label);
}
public sealed partial class Tile : PanelContainer
{
	public static Tile Create(Vector2I position, IColours colours, Action<Vector2I> pressed)
	{
		Tile tile = new Tile { Name = $"Tile (X: {position.X}, Y: {position.Y})" }
			.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		//tile.Button;
		tile.ChangeBackground(position, colours);
		tile.Button.AddThemeColorOverride("font_color", Colors.Transparent);
		tile.Button.AddThemeColorOverride("font_hover_color", Colors.Transparent);
		tile.Button.AddThemeColorOverride("font_focus_color", Colors.Transparent);
		tile.Button.AddThemeColorOverride("font_pressed_color", Colors.Transparent);
		tile.Button.AddThemeFontSizeOverride("font_size", 10);
		//tile.Button.AddThemeConstantOverride("font_size", 1);
		tile.Button.MouseEntered += MouseEntered;
		tile.Button.ButtonDown += Pressed;

		return tile;
		void Pressed() => pressed(position);
		void MouseEntered()
		{
			bool fill = Input.IsMouseButtonPressed(Display.FillButton);
			bool block = Input.IsMouseButtonPressed(Display.BlockButton);
			if (!fill && !block) { return; }
			pressed(position);
		}
	}
	private const MouseButtonMask mask = MouseButtonMask.Left | MouseButtonMask.Right;
	public Button Button { get; } = new Button { Text = Display.EmptyText, ButtonMask = mask }
		.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
	public override void _Ready() => this.Add(Button);

	public void ChangeBackground(Vector2I position, IColours colours)
	{
		const int chunkSize = 5;
		StyleBox baseBox = Button.GetThemeStylebox("normal");
		if (baseBox.Duplicate() is not StyleBoxFlat stylebox) return;
		int chunkIndex = position.X / chunkSize + position.Y / chunkSize;
		Color filledTile = stylebox.BgColor.Blend(colours.NonogramTileBackgroundFilled);
		stylebox.BgColor = Button.Text switch
		{
			Display.FillText => filledTile,
			Display.BlockText => stylebox.BgColor.Darkened(.4f),
			_ => chunkIndex % 2 == 0 ? colours.NonogramTileBackground1 : colours.NonogramTileBackground2
		};
		Button.AddThemeStyleboxOverride("normal", stylebox);
	}
}
