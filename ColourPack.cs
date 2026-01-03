using Godot;

namespace RSG;

public sealed partial class ColourPack : Resource, Nonogram.IColours
{
	public static ColourPack Default { get; } = new ColourPack();

	[Export] public Color MainMenuBackground { get; private set; } = Colors.Black;

	[Export] public Color NonogramBackground { get; private set; } = Colors.DarkOliveGreen;
	[Export] public Color NonogramTimerBackground { get; private set; } = Colors.Burlywood;
	[Export] public Color NonogramHintBackground2 { get; private set; } = Colors.BlanchedAlmond;
	[Export] public Color NonogramHintBackground1 { get; private set; } = Colors.FloralWhite;
	[Export] public Color NonogramTileBackground2 { get; private set; } = Colors.BlanchedAlmond;
	[Export] public Color NonogramTileBackground1 { get; private set; } = Colors.FloralWhite;
	[Export] public Color NonogramTileBackgroundFilled { get; private set; } = Colors.Gold;
}
