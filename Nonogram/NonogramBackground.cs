using Godot;

namespace RSG.Nonogram;

public sealed partial class NonogramBackground : Container
{
	public ColorRect ColorBackground { get; } = new ColorRect { Color = Colors.AntiqueWhite with { A = 0.3f } }
			.Preset(LayoutPreset.FullRect);
	public TextureRect Border { get; } = new TextureRect { Name = "Border", ClipContents = true }
		.Preset(LayoutPreset.FullRect);
	public override void _Ready() => this.Add(ColorBackground, Border);
}