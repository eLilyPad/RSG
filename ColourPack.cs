using Godot;

namespace RSG;

using UI;

public sealed partial class ColourPack : Resource, NonogramDisplay.IColours
{
	[Export] public Color MainMenuBackground { get; private set; } = new Color(0.1f, 0.1f, 0.1f);
	[Export] public Color NonogramBackground { get; private set; } = new Color(0.1f, 0.1f, 0.1f);
}
