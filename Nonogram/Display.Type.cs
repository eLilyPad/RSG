namespace RSG.Nonogram;

using static Display;

public static class DisplayTypeExtensions
{
	public static string AsName(this Type type) => type switch
	{
		Type.Game => "Game",
		Type.Paint => "Paint",
		_ => "Puzzle Display"
	};
	public static Type ChangeType(this PuzzleManager.CurrentPuzzle puzzle, ref Type field, Type value)
	{
		NonogramContainer ui = puzzle.UI;
		ui.Display.Name = value.AsName();
		switch (field)
		{
			case Type.Paint:
				switch (value)
				{
					case Type.Game:
						ui.Container.Remove(false, ui.Studio);
						break;
				}
				break;
			case Type.Game:
				switch (value)
				{
					case Type.Paint:
						ui.Container.Add(ui.Studio);
						int puzzleSize = (int)ui.Studio.PuzzleTab.PuzzleSize.Value;
						string puzzleName = ui.Studio.PuzzleTab.EditableName.Text;
						puzzle.Puzzle = new()
						{
							Name = puzzleName,
							Expected = new(puzzleSize)
						};
						break;
				}
				break;
		}
		return field = value;
	}
	public static Type LogChange(this Type type)
	{
		Console.Console.Log($"Display changed too {type.AsName()}");
		return type;
	}
	public static Type LogCurrent(this Type type)
	{
		Console.Console.Log($"Current Display:  {type.AsName()}");
		return type;
	}
}

public abstract partial class Display
{
	public enum Type { Game, Display, Paint }
}
