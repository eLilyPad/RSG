using Godot;


namespace RSG.Nonogram;

public static class CurrentPuzzleExtensions
{
	//public static CurrentPuzzle Create<TEvents>(this TEvents events, Node parent, ColourPack colours)
	//	where TEvents : IManagePuzzle, PuzzleCompleteScreen.IHandleSignals
	//{
	//	CurrentPuzzle current = new();
	//	NonogramContainer ui = current.UI;
	//	Backgrounded<PuzzleCompleteScreen> completionScreen = ui.CompletionScreen;

	//	parent.AddChild(ui);

	//	ui.CompletionScreen.Value.Signals = events;
	//	ui.Colours = colours;
	//	ui.ChangeColour<CurrentPuzzle>(colours);

	//	Console.Console.Command command = new()
	//	{
	//		Default = () => Console.Console.Log("do nothing, show help"),
	//		Flags = new()
	//		{
	//			["toggle_completion"] = () =>
	//			{
	//				completionScreen.Visible = !completionScreen.Visible;
	//			},
	//		}
	//	};
	//	Console.Console.Add(Core.DefaultCommandPrefix, ("nonogram", command));

	//	return current;
	//}
}

