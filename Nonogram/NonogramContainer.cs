using Godot;

namespace RSG.Nonogram;

public sealed partial class NonogramContainer : PanelContainer
{
	public const int MarginValue = 20;
	public Backgrounded<PuzzleCompleteScreen> CompletionScreen { get; } = new Backgrounded<PuzzleCompleteScreen>
	{
		Name = "PuzzleCompleteScreen",
		Visible = false,
		Background = new ColorRect { Name = "Background", Color = Colors.SlateGray with { A = .7f } }
			.Preset(LayoutPreset.FullRect),
		Value = new PuzzleCompleteScreen { Name = "Value" }
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.Minsize, 250)
	}.Preset(preset: LayoutPreset.Center, resizeMode: LayoutPresetMode.Minsize);

	public NonogramBackground Background { get; } = new NonogramBackground { Name = "Background" }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public Display.Default Display { get; } = new Display.Default { }
		.SizeFlags(both: SizeFlags.ExpandFill);
	public HBoxContainer Container { get; } = new HBoxContainer { Name = "Container" }
		.SizeFlags(both: SizeFlags.ExpandFill);
	public NonogramStudioBar Studio { get; } = new NonogramStudioBar { Name = "Studio", SizeFlagsStretchRatio = .6f }
		.SizeFlags(both: SizeFlags.ExpandFill);
	public MarginContainer Margin = new MarginContainer { Name = "Margin" }
		.SizeFlags(both: SizeFlags.ExpandFill)
		.SetMarginAll(MarginValue);

	public IColours Colours
	{
		private get; set
		{
			Background.ColorBackground.Color = value.NonogramBackground;
			Display.Timer.Background.Color = value.NonogramTimerBackground;
			Tiles.Colours = Hints.Colours = value;
			field = value;
		}
	} = Core.Colours;
	public int PuzzleSize { set => ChangePuzzleSize(value); }

	internal Tile.Pool Tiles { get; }
	internal Hints Hints { get; }

	internal NonogramContainer(Tile.Pool tiles, Hints hints)
	{
		Tiles = tiles;
		Hints = hints;
	}
	public override void _Ready()
	{
		this.Add(
			Background,
			Margin.Add(Container.Add(Display, Studio)),
			CompletionScreen
		);
		PuzzleSize = Nonogram.Display.Data.DefaultSize;
	}
	private void ChangePuzzleSize(int value)
	{
		Hints.Clear();
		Tiles.Clear();
		Tiles.Update(value);
		Hints.TileSize = Tiles.TileSize;
		Hints.Update(value);
		Display.TilesGrid.Columns = value;
		Studio.PuzzleTab.PuzzleSize.SetValueNoSignal(value);
		Display.TilesGrid.CustomMinimumSize = Mathf.CeilToInt(value) * Tiles.TileSize;
	}
}
