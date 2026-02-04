using Godot;

namespace RSG.Nonogram;

public sealed partial class NonogramContainer : PanelContainer
{
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
	public required Display Display { get; init; }
	public IColours Colours { set => ChangeColours(value); }
	//public NonogramContainer(string name, Display display) => (Name, Display) = (name, display);

	public override void _Ready() => this
		.Add(Background, Display, CompletionScreen)
		.Preset(LayoutPreset.FullRect);

	private void ChangeColours(IColours value)
	{
		Background.ColorBackground.Color = value.NonogramBackground;
		Display.Timer.Background.Color = value.NonogramTimerBackground;
	}
}
