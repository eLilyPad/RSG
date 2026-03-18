namespace RSG.Nonogram;

using Console;
using static Display;

public static class CurrentPuzzleExtensions
{
	public static CurrentPuzzle SetColours(this CurrentPuzzle value, IColours colours)
	{
		value.Colours = colours;
		return value;
	}
	public static CurrentPuzzle ConnectSignals(
		this CurrentPuzzle value,
		PuzzleCompleteScreen.IHandleSignals puzzleCompletionHandler,
		IHandleStudioSelector studioSelector,
		ISaveListener? saveListener,
		Action<SaveData> onCompletion
	)
	{
		value.UI.CompletionScreen.Value.Signals = puzzleCompletionHandler;
		value.UI.SelectorSignals = studioSelector;
		value.PuzzleCompleted = onCompletion;
		value.SaveListener = saveListener;
		return value;
	}
	public static CurrentPuzzle AddCommands(this CurrentPuzzle puzzle)
	{
		Console.Command command = new()
		{
			Default = () => puzzle.Type.LogCurrent(),
			Flags = new()
			{
				["game"] = () => (puzzle.Type = Type.Game).LogChange(),
				["paint"] = () => (puzzle.Type = Type.Studio).LogChange(),
			}
		};
		Console.Add(prefix: Core.Prefix, ("nonogram", command));
		return puzzle;
	}
}
