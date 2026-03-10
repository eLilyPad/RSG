using Godot;

namespace RSG.Extensions;

public static class ThemeExtensions
{
	public static MarginContainer SetMarginAll(this MarginContainer margin, int marginValue)
	{
		margin.AddThemeConstantOverride("margin_top", marginValue);
		margin.AddThemeConstantOverride("margin_left", marginValue);
		margin.AddThemeConstantOverride("margin_bottom", marginValue);
		margin.AddThemeConstantOverride("margin_right", marginValue);
		return margin;
	}
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
		var theme = control.GetThemeStylebox(name).Duplicate();
		Assert(theme is TStyle, $"theme is {theme.GetType()}");
		TStyle style = (TStyle)theme;
		modify(style);
		control.AddThemeStyleboxOverride(name, style);
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
