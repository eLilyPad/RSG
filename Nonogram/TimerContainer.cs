using Godot;

namespace RSG.Nonogram;

public sealed partial class TimerContainer : PanelContainer
{
	public RichTextLabel Time { get; } = new RichTextLabel
	{
		Name = "Label",
		FitContent = true,
		HorizontalAlignment = HorizontalAlignment.Center,
		VerticalAlignment = VerticalAlignment.Center,
		BbcodeEnabled = true
	};
	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = Colors.DarkSlateBlue }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public override void _Ready() => this.Add(Background, Time);
}
