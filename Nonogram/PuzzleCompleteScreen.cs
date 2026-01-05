using Godot;

namespace RSG.Nonogram;

public sealed partial class PuzzleCompleteScreen : VBoxContainer
{
	public sealed partial class CompletionOptions : HBoxContainer
	{
		public Button Levels { get; } = new() { Name = "Levels", Text = "Levels" };
		public Button Dialogues { get; } = new() { Name = "Dialogue", Text = "Dialogues" };
		public Button PlayDialogue { get; } = new() { Name = "PlayDialogue", Text = "Play Dialogue" };
		public override void _Ready() => this.Add(Levels, Dialogues, PlayDialogue);
	}
	public sealed partial class CompletionReport : VBoxContainer
	{
		public RichTextLabel Title { get; } = new()
		{
			Name = "title",
			FitContent = true,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			BbcodeEnabled = true,
			Text = "[font_size=50]Unlocked"
		};
		public RichTextLabel Log { get; } = new()
		{
			Name = "Log",
			FitContent = true,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			BbcodeEnabled = true,
		};
		public override void _Ready() => this.Add(Title, Log);
	}
	public CompletionOptions Options { get; } = new CompletionOptions
	{
		Name = "Options",
		SizeFlagsStretchRatio = .3f,
		Alignment = AlignmentMode.Center
	}
		.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ShrinkCenter)
		.Preset(preset: LayoutPreset.Center, resizeMode: LayoutPresetMode.KeepSize);
	//public CompletionReport Report { get; } = new CompletionReport() { SizeFlagsStretchRatio = .3f }
	public Backgrounded<CompletionReport> Report { get; } = new Backgrounded<CompletionReport>()
	{
		Name = "Report",
		SizeFlagsStretchRatio = .3f,
		Background = new ColorRect { Color = Colors.DarkBlue with { A = .5f } }
			.Preset(LayoutPreset.FullRect),
		Value = new CompletionReport() { Name = "Value Report" }
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize)
	}.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill);
	public RichTextLabel CompletionTitle { get; } = new RichTextLabel
	{
		Name = "Completion Title",
		BbcodeEnabled = true,
		Text = "[color=black][font_size=80] Puzzle Complete",
		HorizontalAlignment = HorizontalAlignment.Center,
		VerticalAlignment = VerticalAlignment.Center,
		FitContent = true,
	}
		.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill)
		.Preset(preset: LayoutPreset.Center, resizeMode: LayoutPresetMode.KeepSize);
	public override void _Ready() => this.Add(CompletionTitle, Report, Options);
}
