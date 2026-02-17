namespace RSG.Extensions;

public static class ThemeSetting
{
	public static void ResetTheme(this Nonogram.Display display)
	{
		const int marginValue = 100, spacerValue = 1;
		display.Grid.AddThemeConstantOverride("h_separation", 1);
		display.Grid.AddThemeConstantOverride("v_separation", 1);
		display.Rows.AddThemeConstantOverride("separation", spacerValue);
		display.Columns.AddThemeConstantOverride("separation", spacerValue);
		display.Margin.AddThemeConstantOverride("margin_top", marginValue);
		display.Margin.AddThemeConstantOverride("margin_bottom", marginValue / 2);
		display.TilesGrid.AddThemeConstantOverride("h_separation", 0);
		display.TilesGrid.AddThemeConstantOverride("v_separation", 0);
		display.TilesGrid.AddThemeConstantOverride("h_separation", spacerValue);
		display.TilesGrid.AddThemeConstantOverride("v_separation", spacerValue);
	}
}
