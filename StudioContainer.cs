using Godot;

namespace RSG;

public sealed partial class StudioContainer : Container
{
	private sealed partial class NonogramPainter : Container
	{

	}
	public TabContainer Tabs { get; } = new TabContainer { Name = "Tabs" }
		.Preset(LayoutPreset.FullRect);

	public Container Nonogram { get; } = new NonogramPainter();
	public override void _Ready()
	{
		this.Add(Tabs.Add(Nonogram));
	}

}

