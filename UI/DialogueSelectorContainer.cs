using Godot;
using RSG.Dialogue;

namespace RSG;

public sealed partial class DialogueSelector : PanelContainer
{
	public sealed partial class DialogueDisplay : BoxContainer
	{
		public static DialogueDisplay Create(string name, CanvasItem parent)
		{
			DialogueDisplay display = new DialogueDisplay
			{
				Button = new() { Name = name + " Button", Text = name },
				Background = new ColorRect { Name = "Background", Color = Colors.Beige }
					.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill)
			}.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
			display.Button.Connect(BaseButton.SignalName.Pressed, Callable.From(Pressed));
			void Pressed()
			{
				Dialogues.Start(name);
				parent.Hide();
			}
			return display;
		}
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
	public override void _Ready() => this.Add(Background, Scroll.Add(DisplayContainer));

}
