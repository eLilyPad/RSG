using Godot;

namespace RSG.Nonogram;


public sealed partial class NonogramContainer : PanelContainer
{
	public interface IHave
	{
		NonogramContainer UI { get; }
		void SetTimeText(TimeSpan time) => UI.Display.Timer.Time.Text = "[font_size=30]" + time;
		Container HintsParent(Display.Side side) => side switch
		{
			Display.Side.Row => UI.Display.Rows,
			_ => UI.Display.Columns
		};
	}
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
	public Display Display { get; init; } = new Display.Default();
	public IColours Colours { set => ChangeColours(value); }

	public override void _Ready() => this
		.Add(Background, Display, CompletionScreen)
		.Preset(LayoutPreset.FullRect);

	private void ChangeColours(IColours value)
	{
		Background.ColorBackground.Color = value.NonogramBackground;
		Display.Timer.Background.Color = value.NonogramTimerBackground;
	}
}
