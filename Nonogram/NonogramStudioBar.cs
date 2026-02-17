using Godot;

namespace RSG.Nonogram;

public sealed partial class NonogramStudioBar : Container
{
	public sealed partial class PuzzleTabContainer : VBoxContainer
	{
		public LineEdit EditableName { get; } = new LineEdit { Text = "Name" };
		public SpinBox PuzzleSize { get; } = new SpinBox { Name = "Puzzle Size" };
		public override void _Ready() => this.Add(EditableName);
	}
	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = Colors.AliceBlue }
		.Preset(LayoutPreset.FullRect);
	public TabContainer Tabs { get; } = new TabContainer { Name = "Tabs" }
		.Preset(LayoutPreset.FullRect);
	public PuzzleTabContainer PuzzleTab { get; } = new PuzzleTabContainer { Name = "Puzzle" }
		.Preset(LayoutPreset.FullRect);
	public override void _Ready() => this.Add(Background, Tabs.Add(PuzzleTab));
}
