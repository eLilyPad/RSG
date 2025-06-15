using Godot;

namespace RSG;

public sealed partial class ColourPack : Resource
{
	[Export] public Color MainMenuBackground { get; private set; } = new Color(0.1f, 0.1f, 0.1f);
	[Export] public Color NonogramBackground { get; private set; } = new Color(0.1f, 0.1f, 0.1f);
}
