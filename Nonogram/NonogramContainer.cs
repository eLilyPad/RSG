using Godot;

namespace RSG.Nonogram;

public sealed partial class NonogramContainer : PanelContainer
{
	public interface IHaveTools { PopupMenu Tools { get; } }
	public interface IHaveStatus { StatusBar Status { get; } }

	public sealed partial class DisplayContainer : TabContainer
	{
		public List<Display> Tabs { internal get; init; } = [];
		public Display CurrentTabDisplay => GetCurrentTabControl() is not Display display ? Tabs.First() : display;
	}
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
	public sealed partial class PuzzleCompleteScreen : PanelContainer
	{
		public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = Colors.DarkGray }
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public VBoxContainer Container { get; } = new VBoxContainer { Name = "Completion Container" }
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public HBoxContainer Options { get; } = new HBoxContainer { Name = "Options Container" }
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public Button Levels { get; } = new() { Name = "LevelsButton", Text = "Levels" };
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
		public override void _Ready()
		{
			this.Add(
				Background,
				Container.Add(CompletionTitle, Options.Add(Levels))
			);
		}
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
	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = new(.2f, .3f, 0) }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public VBoxContainer Container { get; } = new VBoxContainer { Name = "Container" }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public Display.Default Display { get; } = new Display.Default { }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public DisplayContainer Displays => field ??= new DisplayContainer { Name = $"{typeof(Display)} Tabs", TabsVisible = false }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	public override void _Ready() => this.Add(Background, Display, CompletionScreen);
}
