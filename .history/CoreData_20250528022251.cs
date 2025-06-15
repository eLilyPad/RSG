using Godot;

namespace RSG;

public sealed partial class CoreData : Resource
{
	[Export] public ColourPack Colours { get; private set; } = new ColourPack();
}
