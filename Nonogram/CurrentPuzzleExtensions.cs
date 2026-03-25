namespace RSG.Nonogram;

using Console;
using RSG.Dialogue;
using static Display;

public static class CurrentPuzzleExtensions
{
	public static CurrentPuzzle SetColours(this CurrentPuzzle value, IColours colours)
	{
		value.Colours = colours;
		return value;
	}
	public static CurrentPuzzle ConnectVisibilityHandler<T>(this CurrentPuzzle value, T handler)
	where T : IHandleStudioSelector
	{
		value.UI.SelectorSignals = handler;
		value.UI.CompletionScreen.Value.VisibilityChanged += () =>
		{
			if (!value.UI.CompletionScreen.Value.Options.PlayDialogue.Visible) return;
			Assert(Dialogues.Contains(value.CompletionDialogueName));
			value.UI.CompletionScreen.Value.Report.Value.Log.Text = $"Dialogue: {value.CompletionDialogueName}";
		};
		return value;
	}
	public static CurrentPuzzle ConnectSignals(
		this CurrentPuzzle value,
		PuzzleCompleteScreen.IHandleSignals puzzleCompletionHandler,
		ISaveListener? saveListener
	)
	{
		value.UI.CompletionScreen.Value.Signals = puzzleCompletionHandler;
		value.PuzzleCompleted = save =>
		{
			value.UI.CompletionScreen.Show();
			Dialogues.Enable(save.Expected.DialogueName);
		};
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
