using Godot;

namespace RSG.Nonogram;

public sealed partial class SoundEffects() : Resource
{
	[Export] public AudioStream TileClicked { get; private set; } = new();
	[Export] public AudioStream PuzzleComplete { get; private set; } = new();
}
