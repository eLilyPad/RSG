using Godot;

namespace RSG;

public sealed partial class DialogueResources : Resource
{
	[Export] public CompressedTexture2D Background { get; private set; } = new();
	[Export] public CompressedTexture2D Profile { get; private set; } = new();
}