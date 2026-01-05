using Godot;

namespace RSG.Extensions;

public static class ThemeExtensions
{
	public static void AddAllFontThemeOverride(this Button button, Color color)
	{
		button.AddThemeColorOverride("font_color", color);
		button.AddThemeColorOverride("font_hover_color", color);
		button.AddThemeColorOverride("font_focus_color", color);
		button.AddThemeColorOverride("font_pressed_color", color);
	}
	public static void OverrideNormalStyle<T>(this Control control, Func<T, T> modify)
	where T : StyleBox
	{
		const string themeName = "normal";
		if (control.GetThemeStylebox(themeName).Duplicate() as T is T style)
		{
			modify(style);
			control.AddThemeStyleboxOverride(themeName, style);
		}
	}
	public static void StyleChequeredButtons(
		this Button button,
		Vector2I position,
		Func<bool, Color> getChunksColor,
		StyleBoxFlat? style = null
	)
	{
		const int chunkSize = 5;
		const string themeName = "normal";
		style ??= button.GetThemeStylebox(themeName).Duplicate() as StyleBoxFlat;
		if (style is null) return;
		int chunkIndex = position.X / chunkSize + position.Y / chunkSize;
		style.BgColor = getChunksColor(chunkIndex % 2 == 0);
		button.AddThemeStyleboxOverride(themeName, style);
	}
}
