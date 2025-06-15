namespace RSG
{
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
}