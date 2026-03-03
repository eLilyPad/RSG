using Godot;

namespace RSG.Nonogram;

public interface IChangePuzzle
{
	void ModifyName(string value);
	void ModifySize(double value);
}
public sealed partial class NonogramStudioBar : Container
{
	private static RichTextLabel CreateTabLabel(string name, string value) => new RichTextLabel
	{
		Name = name,
		FitContent = true,
		Text = value
	}.Preset(LayoutPreset.HcenterWide);

	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = Colors.AliceBlue }
		.Preset(LayoutPreset.FullRect);
	public TabContainer Tabs { get; } = new TabContainer { Name = "Tabs" }
		.Preset(LayoutPreset.FullRect);
	public PuzzleTabContainer PuzzleTab { get; } = new PuzzleTabContainer { Name = "Puzzle" }
		.Preset(LayoutPreset.FullRect);
	public PacksTabContainer PacksTab { get; } = new PacksTabContainer { Name = "Packs" }
		.Preset(LayoutPreset.FullRect);
	public override void _Ready() => this.Add(Background, Tabs.Add(PuzzleTab, PacksTab));

	public sealed partial class PacksTabContainer : VBoxContainer
	{
		public RichTextLabel Header { get; } = CreateTabLabel("Header Label", "Packs");
		public Button SavePuzzle { get; } = new Button { Name = "Save Puzzle", Text = "Save" };
		public PuzzleSelector.Studio Scroll { get; } = new PuzzleSelector.Studio { Name = "Puzzles" }
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public override void _Ready() => this.Add(Header, SavePuzzle, Scroll);
	}
	public sealed partial class PuzzleTabContainer : VBoxContainer
	{
		public LineEdit EditableName { get; } = new LineEdit { Name = "Name" };
		public RichTextLabel Message { get; } = CreateTabLabel("Message Label", "Message");
		public SpinBox PuzzleSize { get; } = new SpinBox
		{
			Name = "Puzzle Size",
			MinValue = 10,
			MaxValue = 20,
			Step = Tile.Pool.ChunkSize,
			AllowLesser = false
		};
		public IChangePuzzle Signals
		{
			set
			{
				if (field is not null)
				{
					EditableName.TextSubmitted -= field.ModifyName;
					PuzzleSize.ValueChanged -= field.ModifySize;
				}
				field = value;
				EditableName.TextSubmitted += field.ModifyName;
				PuzzleSize.ValueChanged += field.ModifySize;
			}
		}
		public override void _Ready() => this.Add(EditableName, PuzzleSize, Message);
	}
}
