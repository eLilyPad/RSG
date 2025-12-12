using Godot;

namespace RSG;

public sealed partial class DialogueResources : Resource
{
	[Export] public CompressedTexture2D Background1 { get; private set; } = new();
	[Export] public CompressedTexture2D Background2 { get; private set; } = new();
	[Export] public CompressedTexture2D Profile { get; private set; } = new();
}