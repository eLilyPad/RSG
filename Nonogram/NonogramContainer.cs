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

	public PuzzleCompleteScreen CompletionScreen { get; } = new PuzzleCompleteScreen
	{
		Name = "PuzzleCompleteScreen",
		Visible = false
	}.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
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

	public override void _Ready() => this.Add(Background, Display, CompletionScreen);
}
