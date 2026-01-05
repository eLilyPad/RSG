using Godot;

namespace RSG.Nonogram;

public sealed partial class GuideLines : Container
{
	public ColorRect BackGround { get; } = new ColorRect { Color = Colors.AntiqueWhite with { A = 0.3f } }
		.Preset(LayoutPreset.FullRect);
	public TextureRect Lines { get; } = new TextureRect { Name = "Lines", ClipContents = true }
		.Preset(LayoutPreset.FullRect);
	public override void _Ready() => this.Add(BackGround, Lines);
}
