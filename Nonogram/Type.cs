namespace RSG.Nonogram;

using static PuzzleManager;

public static class PuzzleTypeExtensions
{

	public static Godot.Color Background(this Type type, IColours colours)
	{
		return type switch
		{
			Type.Paint => colours.NonogramPaintBackground,
			Type.Game => colours.NonogramBackground,
			_ => colours.NonogramBackground,
		};
	}
	public static Type ChangeType<T>(this T a, ref Type field, Type value)
	where T : NonogramContainer.IHave
	{

		if (field == value) return field;
		field = value;
		Display display = a.UI.Display;
		Display.DisplaySpacer spacer = display.Spacer;
		display.Name = field.AsName();
		bool displayingTimer = spacer.HasChild(spacer.Timer);
		switch (field)
		{
			case Type.Game when !displayingTimer:
				spacer.AddChild(spacer.Timer);
				break;
			case Type.Paint or Type.Display when displayingTimer:
				spacer.RemoveChild(spacer.Timer);
				break;
		}
		return field;
	}
}
public interface IDisplayType { Type Type { get; set; } }
public sealed partial class PuzzleManager
{
	public enum Type { Game, Display, Paint }
}
