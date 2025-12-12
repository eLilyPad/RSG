using Godot;

namespace RSG;

using static Dialogue;

public sealed partial class DialogueSelector : PanelContainer
{
	sealed partial class DialogueDisplay : BoxContainer
	{
		public required ColorRect Background { get; init; }
		public required Button Button { get; init; }
		public override void _Ready() => this.Add(Background, Button);
	}

	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = Colors.DarkCyan, }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public ScrollContainer Scroll { get; } = new ScrollContainer()
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public Labelled<VBoxContainer> DisplayContainer { get; } = new Labelled<VBoxContainer>()
	{
		Name = "Dialogues Container",
		Vertical = true,
		Label = new RichTextLabel { Name = "DialoguesTitle", FitContent = true, Text = "Dialogues" }
			.Preset(LayoutPreset.CenterTop, LayoutPresetMode.KeepSize),
		Value = new VBoxContainer()
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	}
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	private readonly List<DialogueDisplay> _displays = [];
	public override void _Ready() => this
		.Add(Background, Scroll.Add(DisplayContainer))
		.LinkToParent(_displays);
	public void Clear() => DisplayContainer.Value.RemoveChildren(true);
	public void Fill(params IEnumerable<string> speeches)
	{
		foreach (string name in speeches)
		{
			DialogueDisplay display = new DialogueDisplay
			{
				Button = new() { Name = name + " Button", Text = name },
				Background = new ColorRect { Name = "Background", Color = Colors.Beige }
					.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill)
			}.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
			display.Button.Pressed += Pressed;
			DisplayContainer.Value.Add(display);

			void Pressed()
			{
				Dialogues.Start(name);
				Hide();
			}
		}
	}
}
