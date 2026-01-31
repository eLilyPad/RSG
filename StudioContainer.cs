using Godot;

namespace RSG;

using Nonogram;

public sealed partial class NonogramPainter : PanelContainer
{
	public NonogramBackground Background { get; } = new NonogramBackground { Name = "Background" }
	.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public Display Display { get; init; } = new Display.Default { }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);

}
public sealed partial class StudioContainer : Container
{
	public TabContainer Tabs { get; } = new TabContainer { Name = "Tabs" }
		.Preset(LayoutPreset.FullRect);

	public override void _Ready()
	{
		AddChild(Tabs);
	}

}

