using Godot;

namespace RSG.Nonogram;

public interface IColours
{
	Color NonogramBackground { get; }
	Color NonogramFilledBorder { get; }
	Color NonogramBlockedBorder { get; }
	Color NonogramTimerBackground { get; }
	Color NonogramHintBackground1 { get; }
	Color NonogramHintBackground2 { get; }
	Color NonogramTileBackground2 { get; }
	Color NonogramTileBackground1 { get; }
	Color NonogramTileBackgroundFilled { get; }
}
