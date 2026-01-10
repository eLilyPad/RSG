using Godot;

namespace RSG.Nonogram;

using Mode = Display.TileMode;

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

	Color NonogramTileBackground(Mode mode, bool alternative)
	{
		Color
		filled = NonogramTileBackgroundFilled,
		background = alternative ? NonogramTileBackground1 : NonogramTileBackground2,
		blocked = background.Darkened(.2f);

		filled = alternative ? filled : filled.Darkened(.2f);

		return mode switch { Mode.Filled => filled, Mode.Blocked => blocked, _ => background };
	}
	Color NonogramLockedBorder(Mode mode) => mode switch
	{
		Mode.Filled => NonogramFilledBorder,
		_ => NonogramBlockedBorder
	};
}
