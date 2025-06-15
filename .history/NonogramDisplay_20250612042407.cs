using Godot;

namespace RSG.UI;

using HintContainers = (BoxContainer Rows, BoxContainer Columns);

public abstract partial class NonogramDisplay : Container
{
	public enum HintType { Row = 1, Column = 0 }
	public const string BlockText = "X", FillText = "O", EmptyText = " ";
	private static Vector2 TileSize = new(40, 40);



	public int TilesLength
	{
		get; init
		{
			ChangeLength(value);
			field = value;
		}
	} = 5;

	public Dictionary<Vector2I, Button> Tiles { get; } = [];
	public Dictionary<HintType, RichTextLabel[]> Hints { get; private set; } = [];

	public GridContainer TilesContainer => field ??= new() { Name = "Tiles", Columns = TilesLength };
	public ColorRect Background { get; } = new() { Name = "Background" };
	public Control Spacer { get; } = new() { Name = "Spacer" };
	public GridContainer Main { get; } = new GridContainer { Name = "MainContainer", Columns = 2 }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public HintContainers HintContainers { get; } = (
		new VBoxContainer { Name = "RowHints" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize),
		new HBoxContainer { Name = "ColumnHints" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize)
	);

	public override void _Ready()
	{
		this.Add(
				Background,
				Main.Add(Spacer, HintContainers.Columns, HintContainers.Rows, TilesContainer)
			)
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.BottomRight, resizeMode: LayoutPresetMode.KeepSize)
			.Name = nameof(NonogramDisplay);
		TilesContainer
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		Spacer
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		Background
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize)
			.CustomMinimumSize = Size;
		Main
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

		Hints.
	}

	public abstract void OnTilePressed(Vector2I position, Button button);
	public abstract void UpdateSettings();

	private int ChangeLength(int value)
	{
		value = value < 1 ? 2 : value;
		Tiles.FreeAll(TilesContainer);

		foreach (Vector2I position in (Vector2I.One * value).AsRange())
		{
			var button = Tiles[position] = Tile();
			TilesContainer.AddChild(button);
			button.Pressed += () => OnTilePressed(position, button);

			Button Tile() => new Button
			{
				Name = $"Button {position}",
				Text = EmptyText,
				CustomMinimumSize = TileSize
			}
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		}

		foreach (int i in Range(0, count: value))
		{
			HintContainers.Rows.Add(Hints[new((int)HintType.Row, i)] = Hint());
			HintContainers.Columns.Add(Hints[new((int)HintType.Column, i)] = Hint());

			RichTextLabel Hint() => new RichTextLabel
			{
				Text = "0",
				FitContent = true,
				CustomMinimumSize = TileSize
			}
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
		}
		return TilesContainer.Columns = value;
	}
	private void DisplayHint(string hint, HintType type, int position)
	{
		//if (Hints.TryGetValue())
		Hints[(int)type][position].Text = hint;
	}

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
