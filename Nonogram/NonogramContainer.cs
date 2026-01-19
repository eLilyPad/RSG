using Godot;

namespace RSG.Nonogram;

public sealed partial class NonogramContainer : PanelContainer
{
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
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);

	public IColours Colours
	{
		private get; set
		{
			Background.ColorBackground.Color = value.NonogramBackground;
			Display.Timer.Background.Color = value.NonogramTimerBackground;
			field = value;
		}
	}
	public int PuzzleSize
	{
		set
		{
			Tiles.Update(value);
			Hints.TileSize = Tiles.TileSize;
			Hints.Update(value);
			Display.TilesGrid.CustomMinimumSize = Mathf.CeilToInt(value) * Tiles.TileSize;
		}
	}

	internal Tile.Pool Tiles { get; init; }
	internal Hints Hints { get; init; }

	internal NonogramContainer(IColours colours, List<Func<Vector2I, bool>> rules, PuzzleManager.CurrentPuzzle puzzle)
	{
		Colours = colours;
		Hints = new(Provider: puzzle, Colours: colours);
		Tiles = new(Provider: puzzle, Colours: colours) { LockRules = new() { Rules = rules } };
	}
	public override void _Ready() => this.Add(Background, Display, CompletionScreen);
}
