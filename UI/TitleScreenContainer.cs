using Godot;

namespace RSG;

public sealed partial class TitleScreenContainer : PanelContainer
{
	public ColorRect Background { get; } = new ColorRect
	{
		Name = "Background",
		Color = Colors.Aquamarine,
	}.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public RichTextLabel LoadingText { get; } = new RichTextLabel
	{
		Name = "Loading Text",
		BbcodeEnabled = true,
		Text = "[color=black][font_size=60] Press Anything To Continue...",
		HorizontalAlignment = HorizontalAlignment.Center,
		VerticalAlignment = VerticalAlignment.Center,
		FitContent = true,
	}.Preset(preset: LayoutPreset.Center, resizeMode: LayoutPresetMode.KeepSize);
	public override void _Ready()
	{
		this.Add(Background, LoadingText);
	}
}

