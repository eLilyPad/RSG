using Godot;
using static Godot.BoxContainer;

namespace RSG.Nonogram;

public sealed partial class PuzzleCompleteScreen : PanelContainer
{
	public sealed partial class CompletionOptions : HBoxContainer
	{
		public Button Levels { get; } = new() { Name = "Levels", Text = "Levels" };
		public Button Dialogues { get; } = new() { Name = "Dialogue", Text = "Dialogues" };
		public Button PlayDialogue { get; } = new() { Name = "PlayDialogue", Text = "Play Dialogue" };
		public override void _Ready() => this.Add(Levels, Dialogues, PlayDialogue);
	}
	public sealed partial class CompletionReport : HBoxContainer
	{
		public sealed partial class Icon : AspectRatioContainer
		{
			public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = Colors.SeaGreen }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
			public TextureRect Image { get; } = new TextureRect { Name = "Image" }
				.Preset(LayoutPreset.FullRect);

			public override void _Draw() => this.Add(Image);
		}
	}
	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = Colors.DarkGray }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public VBoxContainer Container { get; } = new VBoxContainer { Name = "Completion Container" }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public CompletionOptions Options { get; } = new CompletionOptions { Name = "Options", Alignment = AlignmentMode.Center }
		.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.Fill)
		.Preset(preset: LayoutPreset.Center, resizeMode: LayoutPresetMode.KeepSize);
	public CompletionReport Report { get; } = new CompletionReport()
		.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.Fill)
		.Preset(preset: LayoutPreset.BottomRight, resizeMode: LayoutPresetMode.KeepSize);
	public RichTextLabel CompletionTitle { get; } = new RichTextLabel
	{
		Name = "Completion Title",
		BbcodeEnabled = true,
		Text = "[color=black][font_size=60] Puzzle Complete",
		HorizontalAlignment = HorizontalAlignment.Center,
		VerticalAlignment = VerticalAlignment.Center,
		FitContent = true,
	}
		.Preset(preset: LayoutPreset.Center, resizeMode: LayoutPresetMode.KeepSize);
	public override void _Ready() => this.Add(Background, Container.Add(CompletionTitle, Options));
}
