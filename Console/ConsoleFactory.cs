namespace RSG.Console;

using Nonogram;
using Minesweeper;

public static class ConsoleFactory
{
	public static TConfig AddNonogramCommands<TConfig>(this TConfig config)
	where TConfig : NonogramContainer.IHave
	{
		Backgrounded<PuzzleCompleteScreen> completionScreen = config.UI.CompletionScreen;
		Console.Command command = new()
		{
			Default = () => Console.Log("do nothing, show help"),
			Flags = new()
			{
				["toggle_completion"] = () => completionScreen.Visible = !completionScreen.Visible,
			}
		};
		Console.Add(Core.DefaultCommandPrefix, ("nonogram", command));
		return config;
	}
	public static TConfig AddMinesweeperCommands<TConfig>(this TConfig config)
	where TConfig : MinesweeperContainer.IHave, Minesweeper.ICurrentPuzzle
	{
		Console.Command command = new()
		{
			Flags = new()
			{
				["new"] = () =>
				{
					config.Puzzle = Manager.Data.CreateRandom(10);
					config.UI.Show();
					Console.Log("Started new Minesweeper game");
				},
				["uncover_all"] = () =>
				{
					config.UI.Tiles.ShowAll();
					config.UI.Show();
					Console.Log("Uncovering all tiles");
				}
			}
		};
		Console.Add(Core.DefaultCommandPrefix, ("minesweeper", command));
		return config;
	}
}

