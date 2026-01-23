using Godot;

namespace RSG.Minesweeper;

public sealed partial class MinesweeperTextures : Resource
{
	[Export] public CompressedTexture2D Flag { get; private set; } = new();
	[Export] public CompressedTexture2D Bomb { get; private set; } = new();
}

