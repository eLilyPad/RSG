namespace RSG.Nonogram;

using Godot;
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
		if (field == value) return field;

		NonogramStudioBar studio = puzzle.UI.Studio;
		HBoxContainer container = puzzle.UI.Container;
		Default display = puzzle.UI.Display;
		display.Name = value.AsName();

		switch (value)
		{
			case Type.Game:
				display.Timer.Show();
				container.Remove(false, studio);
				break;
			case Type.Paint:
				display.Timer.Hide();
				container.Add(studio);
				puzzle.Puzzle = new() { Expected = new() };
				break;
		}

		return field = value;
	}
	public static Type LogChange(this Type type)
	{
		Console.Console.Log($"Display changed too: {type.AsName()}");
		return type;
	}
	public static Type LogCurrent(this Type type)
	{
		Console.Console.Log($"Current Display: {type.AsName()}");
		return type;
	}
}

public abstract partial class Display
{
	public enum Type { Game, Paint }
}
