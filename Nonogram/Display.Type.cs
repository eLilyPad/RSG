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
	public static void ChangeType(this NonogramContainer nonogram, ref Type field, Type value)
	{
		nonogram.Display.Name = value.AsName();
		switch (field)
		{
			case Type.Paint when value is Type.Game:
				nonogram.RemoveChild(nonogram.Studio);
				break;
			case Type.Game when value is Type.Paint:
				nonogram.AddChild(nonogram.Studio);
				break;
		}
		field = value;
	}
}

public abstract partial class Display
{
	public enum Type { Game, Display, Paint }
}
