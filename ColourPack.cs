using Godot;

namespace RSG;

public sealed partial class ColourPack : Resource, UI.Nonogram.IColours
{
	[Export] public Color MainMenuBackground { get; private set; } = new Color(0.1f, 0.1f, 0.1f);
	[Export] public Color NonogramBackground { get; private set; } = new Color(0.1f, 0.1f, 0.1f);
}
