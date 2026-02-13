using Godot;

namespace RSG.Nonogram;

using static NonogramContainer;
using PuzzleType = PuzzleManager.Type;

public static class NonogramContainerExtensions
{
	public static string SetTimeText<T>(this T a, TimeSpan time) where T : IHave
	{
		string text = $"[font_size=30]{time}";
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
	//public static PuzzleType ChangeType<T>(this T a, ref PuzzleType field, PuzzleType value) where T : IHave
	//{
	//	if (field == value) return field;
	//	field = value;
	//	Display display = a.UI.Display;
	//	Display.DisplaySpacer spacer = display.Spacer;
	//	display.Name = field.AsName();
	//	bool displayingTimer = spacer.HasChild(spacer.Timer);
	//	switch (field)
	//	{
	//		case PuzzleType.Game when !displayingTimer:
	//			spacer.AddChild(spacer.Timer);
	//			break;
	//		case PuzzleType.Paint or PuzzleType.Display when displayingTimer:
	//			spacer.RemoveChild(spacer.Timer);
	//			break;
	//	}
	//	return field;
	//}
}
