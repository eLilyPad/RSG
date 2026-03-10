namespace RSG.Nonogram;

using static Display;

public static class DisplayTypeExtensions
{
	public static string AsName(this Type type) => type switch
	{
		Type.Game => "Game",
		Type.Studio => "Paint",
		_ => "Puzzle Display"
	};
	public static Data HintsData(this Type type, SaveData save) => type switch
	{
		Type.Game => save.Expected,
		_ => save
	};
	public static Data InputData(this Type type, SaveData save) => type switch
	{
		Type.Studio => save.Expected,
		_ => save
	};
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
	public enum Type { Game, Studio }
}
