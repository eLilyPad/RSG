using Godot;

namespace RSG.Nonogram;

using static NonogramContainer;
using PuzzleType = PuzzleManager.Type;

public static class NonogramContainerExtensions
{
	public static string SetTimeText<T>(this T a, TimeSpan time) where T : IHave
	{
		string text = $"[font_size=30]{(int)time.TotalHours:00}:{time: mm\\:ss}";
		return a.UI.Display.Spacer.Timer.Time.Text = text;
	}
	public static Node HintsParent<T>(this T a, Display.Side side) where T : IHave
	{
		Assert(side is Display.Side.Row or Display.Side.Column);
		return side switch
		{
			Display.Side.Row => a.UI.Display.Rows,
			Display.Side.Column => a.UI.Display.Columns
		};
	}
	public static TConfig ChangeEvents<TConfig, TEvents>(this TConfig config, TEvents events)
	where TConfig : IHave, IHavePuzzleEvents
	where TEvents : IManagePuzzle, PuzzleCompleteScreen.IHandleSignals
	{
		config.EventHandler = events;
		config.UI.CompletionScreen.Value.Signals = events;
		return config;
	}
	public static PuzzleType ChangeType<T>(this T a, ref PuzzleType field, PuzzleType value) where T : IHave
	{
		if (field == value) return field;
		field = value;
		Display display = a.UI.Display;
		Display.DisplaySpacer spacer = display.Spacer;
		display.Name = field.AsName();
		bool displayingTimer = spacer.HasChild(spacer.Timer);
		switch (field)
		{
			case PuzzleType.Game when !displayingTimer:
				spacer.AddChild(spacer.Timer);
				break;
			case PuzzleType.Paint or PuzzleType.Display when displayingTimer:
				spacer.RemoveChild(spacer.Timer);
				break;
		}
		return field;
	}
}

public sealed partial class NonogramContainer : PanelContainer
{
	public interface IHave { NonogramContainer UI { get; } }
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

	public override void _Ready() => this
		.Add(Background, Display, CompletionScreen)
		.Preset(LayoutPreset.FullRect);
}
