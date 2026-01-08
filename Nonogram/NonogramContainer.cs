using Godot;

namespace RSG.Nonogram;

public sealed partial class NonogramContainer : PanelContainer
{
	public interface IHaveTools { PopupMenu Tools { get; } }
	public interface IHaveStatus { StatusBar Status { get; } }

	public sealed partial class StatusBar : HBoxContainer
	{
		public const string PuzzleComplete = "Puzzle is complete", PuzzleIncomplete = "Puzzle is incomplete";
		public RichTextLabel CompletionLabel { get; } = new RichTextLabel
		{
			Name = "Completion",
			FitContent = true,
			CustomMinimumSize = new(200, 0),
			Text = PuzzleIncomplete
		}.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
		public override void _Ready() => this.Add(CompletionLabel);
	}
	public sealed partial class GameDisplay : Display, IHaveTools
	{
		public PopupMenu Tools { get; } = new() { Name = "Game" };
		public required StatusBar Status { get; init; }

	}
	public sealed partial class PaintDisplay : Display, IHaveTools
	{
		public PopupMenu Tools { get; } = new() { Name = "Paint" };
	}

	public Backgrounded<PuzzleCompleteScreen> CompletionScreen { get; } = new Backgrounded<PuzzleCompleteScreen>
	{
		Name = "PuzzleCompleteScreen",
		Visible = false,
		Background = new ColorRect { Name = "Background", Color = Colors.SlateGray with { A = .7f } }
			.Preset(LayoutPreset.FullRect),
		Value = new PuzzleCompleteScreen { Name = "Value" }
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.Minsize, 250)
	}.Preset(preset: LayoutPreset.Center, resizeMode: LayoutPresetMode.Minsize);

	public Menu ToolsBar { get; } = new Menu { Name = "Toolbar", SizeFlagsStretchRatio = 0.05f }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public StatusBar Status { get; } = new StatusBar { Name = "Status Bar", SizeFlagsStretchRatio = 0.05f }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.Preset(LayoutPreset.BottomWide, LayoutPresetMode.KeepWidth);
	public NonogramBackground Background { get; } = new NonogramBackground { Name = "Background" }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public VBoxContainer Container { get; } = new VBoxContainer { Name = "Container" }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public Display.Default Display { get; } = new Display.Default { }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);

	internal Tile.Pool Tiles { get; init; }
	internal Hints Hints { get; init; }

	internal NonogramContainer(Tile.Pool tiles, Hints hints)
	{
		Hints = hints;
		Tiles = tiles;
	}
	public override void _Ready() => this.Add(Background, Display, CompletionScreen);
}
