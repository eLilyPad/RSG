using Godot;

namespace RSG;

public sealed partial class Data : Resource
{
	[Export] public ColourPack Colours { get; private set; } = new ColourPack();
}
