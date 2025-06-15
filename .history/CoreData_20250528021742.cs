using Godot;

namespace RSG;

public sealed partial class CoreData : Resource
{
	[Export] public ColourPack Colours { get; private set; } = new ColourPack();
}

public sealed partial class ColourPack : Resource
{
	[Export] public Color MainMenuBackground { get; private set; } = new Color(0.1f, 0.1f, 0.1f);
	[Export] public Color Background { get; private set; } = new Color(0.1f, 0.1f, 0.1f);
}
