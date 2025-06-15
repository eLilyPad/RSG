namespace RSG;

public sealed class ButtonGrid : Container
{

}

public static class UI
{
	public static MarginContainer Margin(int margin = 30, string name = "Margin")
	{
		MarginContainer container = new()
		{
			Name = name,
			SizeFlagsHorizontal = Container.SizeFlags.ExpandFill,
			SizeFlagsVertical = Container.SizeFlags.ExpandFill
		};
		container.SetAnchorsAndOffsetsPreset(
			preset: Container.LayoutPreset.FullRect,
			resizeMode: Container.LayoutPresetMode.KeepSize,
			margin: margin
		);
		return container;
	}
	public static ColorRect Background(Color color, string name = "Background")
	{
		ColorRect rect = new()
		{
			Name = name,
			Color = color,
			SizeFlagsHorizontal = Container.SizeFlags.ExpandFill,
			SizeFlagsVertical = Container.SizeFlags.ExpandFill
		};
		rect.SetAnchorsAndOffsetsPreset(
			preset: Container.LayoutPreset.FullRect,
			resizeMode: Container.LayoutPresetMode.KeepSize
		);
		return rect;
	}
}

public sealed class MainMenu : Container
{
	public sealed class Buttons<T> : T where T : Container
	{
		public Buttons()
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill;
			SizeFlagsVertical = SizeFlags.ExpandFill;
			SetAnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);
		}
	}
	public override void _Ready()
	{
		var margin = new MarginContainer
		{
			Name = "MarginBackground",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		}.SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: 30
		);
		var background = new ColorRect
		{
			Name = "Background",
			Color = new Color(0.1f, 0.1f, 0.1f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		}.SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		this.Add(
			background,
			margin.Add(
				UI.Background(color: new Color(0.2f, 0.2f, 0.2f), name: "MarginBackground")
			)
		);
	}
}