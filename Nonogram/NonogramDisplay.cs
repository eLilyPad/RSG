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
	public const int TileSize = 31;
	// [Bug] ^^ if this changes the tile becomes a rectangle, with the height being larger than the width
	public const MouseButton FillButton = MouseButton.Left, BlockButton = MouseButton.Right;
	public enum TileMode : int { Block = 2, Fill = 1, Clear = 0 }
	public enum Side { Row, Column }

	public static TileMode PressedMode => Input.IsMouseButtonPressed(BlockButton) ? TileMode.Block
		: Input.IsMouseButtonPressed(FillButton) ? TileMode.Fill
		: TileMode.Clear;

	public IColours Colours { private get; set; } = ColourPack.Default;

	public GridContainer TilesGrid { get; } = new GridContainer { Name = "Tiles", Columns = 2 }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public Container Spacer { get; } = new Container { Name = "Spacer" }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public GridContainer Grid { get; } = new GridContainer { Name = "MainContainer", Columns = 2 }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public VBoxContainer Rows { get; } = new VBoxContainer { Name = "RowHints" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.Expand)
		.Preset(preset: LayoutPreset.CenterRight, resizeMode: LayoutPresetMode.KeepSize);
	public HBoxContainer Columns { get; } = new HBoxContainer { Name = "ColumnHints" }
		.SizeFlags(horizontal: SizeFlags.Expand, vertical: SizeFlags.ExpandFill)
		.Preset(preset: LayoutPreset.CenterBottom, resizeMode: LayoutPresetMode.KeepSize);
	public Container TilesContainer { get; } = new Container { Name = "Tiles Container" }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	protected Dictionary<Vector2I, Tile> Tiles { get; } = [];
	protected Dictionary<HintPosition, Hint> Hints { get; } = [];

	public override sealed void _Ready()
	{
		this.Add(Grid.Add(Spacer, Columns, Rows, TilesContainer.Add(TilesGrid)))
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
	protected virtual void ResetHint(Hint hint) => hint.Text = EmptyHint;
}
public sealed partial class Hint : RichTextLabel
{
	public static Hint Create(Display.HintPosition position, IColours colours)
	{
		Hint hint = new()
		{
			Name = $"Hint (Side: {position.Side}, Index: {position.Index})",
			Text = Display.EmptyHint,
			FitContent = true,
			CustomMinimumSize = Vector2.One * Display.TileSize,
		};
		(hint.HorizontalAlignment, hint.VerticalAlignment) = position.Alignment();
		return hint;
	}
	private Hint() { }
}
public sealed partial class Tile : AspectRatioContainer
{
	public static Tile Create(Vector2I position, IColours colours, Action<Vector2I> pressed)
	{
		Tile button = new() { Name = $"Tile (X: {position.X}, Y: {position.Y})" };
		StyleBox baseBox = button.Button.GetThemeStylebox("normal");
		StyleBoxFlat? stylebox = baseBox.Duplicate() as StyleBoxFlat;
		if (stylebox is not null)
		{
			stylebox.BgColor = position switch
			{
				_ when (position.X / 5 + position.Y / 5) % 2 == 0 => colours.NonogramTileBackground1,
				_ => colours.NonogramTileBackground2
			};
			button.Button.AddThemeStyleboxOverride("normal", stylebox);
		}
		button.Button.MouseEntered += MouseEntered;
		button.Button.ButtonDown += Pressed;

		return button;
		void Pressed() => pressed(position);
		void MouseEntered()
		{
			bool fill = Input.IsMouseButtonPressed(Display.FillButton);
			bool block = Input.IsMouseButtonPressed(Display.BlockButton);
			if (!fill && !block) { return; }
			pressed(position);
		}
	}
	public Button Button { get; } = new()
	{
		Text = Display.EmptyText,
		CustomMinimumSize = Vector2.One * Display.TileSize,
		ButtonMask = MouseButtonMask.Left | MouseButtonMask.Right
	};
	public override void _Ready() => this.Add(Button);
}
