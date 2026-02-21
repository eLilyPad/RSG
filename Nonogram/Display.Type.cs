namespace RSG.Nonogram;

using System.Numerics;
using static Display;

public static class DisplayTypeExtensions
{
	public static string AsName(this Type type) => type switch
	{
		Type.Game => "Game",
		Type.Paint => "Paint",
		_ => "Puzzle Display"
	};
	public static Data HintsData(this Type type, SaveData save) => type switch
	{
		Type.Game => save.Expected,
		_ => save
	};
	public static Data InputData(this Type type, SaveData save) => type switch
	{
		Type.Paint => save.Expected,
		_ => save
	};
	public static void HandleInput(this Type type, NonogramContainer ui, Godot.Vector2I position)
	{
		Tile.Pool tiles = ui.Tiles;
		Hints hints = ui.Hints;
		switch (type)
		{
			case Type.Game:
				_ = tiles.TryLock(position);
				break;
			case Type.Paint:
				hints.Refresh();
				break;
		}
	}

	public static Type ChangeType(this PuzzleManager.CurrentPuzzle puzzle, ref Type field, Type value)
	{
		if (field == value) return field;

		NonogramStudioBar studio = puzzle.UI.Studio;
		var container = puzzle.UI.Container;
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

	public static Type ChangeType(this PuzzleManager.CurrentPuzzle puzzle, Type previous, Type current)
	{
		if (previous == current) return previous;
		NonogramStudioBar studio = puzzle.UI.Studio;
		Default display = puzzle.UI.Display;
		display.Name = current.AsName();
		switch (current)
		{
			case Type.Game:
				display.Timer.Show();
				studio.Hide();
				if (previous is Type.Paint) puzzle.ClearPuzzle();
				break;
			case Type.Paint:
				display.Timer.Hide();
				studio.Show();
				puzzle.Puzzle = new() { Expected = new() };
				break;
		}
		return current;
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
