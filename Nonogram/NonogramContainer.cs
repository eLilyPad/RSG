using Godot;

namespace RSG.Nonogram;

public interface IHandleStudioSelector
{
	void StudioPuzzleSelectorVisibilityChanged();
}

public sealed partial class NonogramContainer : PanelContainer
{
	public const int MarginValue = 20;
	public Backgrounded<PuzzleCompleteScreen> CompletionScreen { get; } = new Backgrounded<PuzzleCompleteScreen>
	{
		Name = "PuzzleCompleteScreen",
		Visible = false,
		Background = new ColorRect { Name = "Background", Color = Colors.SlateGray with { A = .7f } }
			.Preset(LayoutPreset.FullRect),
		Value = new PuzzleCompleteScreen { Name = "Value" }
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.Minsize, 250)
	}.Preset(preset: LayoutPreset.Center, resizeMode: LayoutPresetMode.Minsize);

	public NonogramBackground Background { get; } = new NonogramBackground { Name = "Background" }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public Display.Default Display { get; } = new Display.Default { }
		.SizeFlags(both: SizeFlags.ExpandFill);
	public HBoxContainer Container { get; } = new HBoxContainer { Name = "Container" }
		.SizeFlags(both: SizeFlags.ExpandFill);
	public NonogramStudioBar Studio { get; } = new NonogramStudioBar { Name = "Studio", SizeFlagsStretchRatio = .6f }
		.SizeFlags(both: SizeFlags.ExpandFill);
	public MarginContainer Margin = new MarginContainer { Name = "Margin" }
		.SizeFlags(both: SizeFlags.ExpandFill)
		.SetMarginAll(MarginValue);

	public IColours BackgroundColours
	{
		private get; set
		{
			Background.ColorBackground.Color = value.NonogramBackground;
			Display.Timer.Background.Color = value.NonogramTimerBackground;
			field = value;
		}
	} = Core.Colours;

	public IHandleStudioSelector SelectorSignals
	{
		set
		{
			var previous = field;
			field = value;
			Studio.VisibilityChanged += value.StudioPuzzleSelectorVisibilityChanged;
			if (previous is null) return;
			Studio.VisibilityChanged -= previous.StudioPuzzleSelectorVisibilityChanged;
		}
	}

	public override void _Ready() => this.Add(
		Background,
		Margin.Add(Container.Add(Display, Studio)),
		CompletionScreen
	);
}