using Godot;

namespace RSG.Nonogram;

public interface IChangePuzzle
{
	void ModifyName(string value);
	void ModifySize(double value);
}
public sealed partial class NonogramStudioBar : Container
{
	public sealed partial class PuzzleTabContainer : VBoxContainer
	{
		public LineEdit EditableName { get; } = new LineEdit { Text = "Name" };
		public SpinBox PuzzleSize { get; } = new SpinBox
		{
			Name = "Puzzle Size",
			MinValue = 10,
			AllowLesser = false
		};
		public IChangePuzzle Signals
		{
			set
			{
				if (field is not null)
				{
					EditableName.TextChanged -= field.ModifyName;
					PuzzleSize.ValueChanged -= field.ModifySize;
				}
				field = value;
				EditableName.TextChanged += field.ModifyName;
				PuzzleSize.ValueChanged += field.ModifySize;
			}
		}
		public override void _Ready() => this.Add(EditableName, PuzzleSize);
	}
	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = Colors.AliceBlue }
		.Preset(LayoutPreset.FullRect);
	public TabContainer Tabs { get; } = new TabContainer { Name = "Tabs" }
		.Preset(LayoutPreset.FullRect);
	public PuzzleTabContainer PuzzleTab { get; } = new PuzzleTabContainer { Name = "Puzzle" }
		.Preset(LayoutPreset.FullRect);
	public override void _Ready() => this.Add(Background, Tabs.Add(PuzzleTab));
}
