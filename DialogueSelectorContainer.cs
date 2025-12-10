using Godot;

namespace RSG;

using static Dialogue;

public sealed partial class DialogueSelectorContainer : VBoxContainer
{
	public sealed partial class Display(Labelled<VBoxContainer> Puzzles) : AspectRatioContainer
	{
		public override void _Ready()
		{

		}
	}
	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = Colors.DarkCyan, }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public ScrollContainer Scroll { get; } = new ScrollContainer()
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public Labelled<VBoxContainer> Dialogues { get; } = new Labelled<VBoxContainer>()
	{
		Name = "Puzzles Container",
		Vertical = true,
		Label = new RichTextLabel { Name = "DialoguesTitle", FitContent = true, Text = "Dialogues" }
			.Preset(LayoutPreset.CenterTop, LayoutPresetMode.KeepSize),
		Value = new VBoxContainer()
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	}
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	private readonly List<Display> _displays = [];
	public override void _Ready()
	{
		this.Add(Background, Scroll.Add(Dialogues));

		ChildEnteredTree += OnChildEnteredTree;
		ChildExitingTree += OnChildExitingTree;

		void OnChildExitingTree(Node node)
		{
			if (node is Display display && _displays.Contains(display))
			{
				_displays.Remove(display);
			}
		}
		void OnChildEnteredTree(Node node)
		{
			if (node is Display display)
			{
				_displays.Add(display);
			}
		}
	}
	public void Clear() => Dialogues.Value.Remove(true, _displays);
	public void Fill(params IEnumerable<(string name, Speech)> speeches)
	{

	}
}
