using Godot;

namespace RSG.Nonogram;

public sealed partial class Hint : PanelContainer
{
	public static Hint Create(Display.HintPosition position, IColours colours)
	{
		Hint hint = new Hint
		{
			Name = $"Hint (Side: {position.Side}, Index: {position.Index})",
			Label = new RichTextLabel
			{
				Name = "Label",
				Text = Display.EmptyHint,
				FitContent = true,
			}.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill)
		}.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		(hint.Label.HorizontalAlignment, hint.Label.VerticalAlignment) = position.Alignment();
		hint.Label.AddThemeFontSizeOverride("normal_font_size", 15);
		hint.Background.Color = position.Index % 2 == 0 ? colours.NonogramHintBackground1 : colours.NonogramHintBackground2;
		return hint;
	}
	public required RichTextLabel Label { get; init; }
	public ColorRect Background { get; } = new ColorRect { Name = "Background" }
		.Preset(LayoutPreset.FullRect);
	private Hint() { }
	public override void _Ready() => this.Add(Background, Label);
}
