using Godot;

namespace RSG.Extensions;

public static class ThemeExtensions
{
	public static Button AddAllFontThemeOverride(this Button button, Color color)
	{
		button.AddThemeColorOverride("font_color", color);
		button.AddThemeColorOverride("font_hover_color", color);
		button.AddThemeColorOverride("font_focus_color", color);
		button.AddThemeColorOverride("font_pressed_color", color);
		return button;
	}
	public static TControl OverrideStyle<TStyle, TControl>(
		this TControl control,
		Func<TStyle, TStyle> modify,
		string name = "normal"
	)
	where TStyle : StyleBox
	where TControl : Control
	{
		if (control.GetThemeStylebox(name).Duplicate() as TStyle is TStyle style)
		{
			modify(style);
			control.AddThemeStyleboxOverride(name, style);
		}
		return control;
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
