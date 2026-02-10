using Godot;

namespace RSG.Nonogram;

using Mode = Display.TileMode;

public static class ColourExtensions
{
	private static readonly IColours _backup = Core.Colours;
	public static T SetColours<T>(this T a, IColours? value = null) where T : NonogramContainer.IHave
	{
		value ??= _backup;
		a.UI.Background.ColorBackground.Color = value.NonogramBackground;
		a.UI.Display.Spacer.Timer.Background.Color = value.NonogramTimerBackground;
		return a;
	}
	public static Tile SetColours(this Tile tile, IColours? value = null)
	{
		value ??= _backup;
		bool isAlternative = tile.IsAlternative;
		Mode mode = tile.Mode;
		Color tileColour = value.NonogramTileBackground(mode, alternative: isAlternative);
		Color lockedColour = value.NonogramLockedBorder(mode);
		tile.Button.OverrideStyle(modify: (StyleBoxFlat style) =>
		{
			style.BorderColor = lockedColour;
			style.BgColor = tileColour;
			return style;
		});
		tile.Button.OverrideStyle(name: "hover", modify: (StyleBoxFlat style) =>
		{
			style.BgColor = tileColour;
			return style;
		});
		return tile;
	}
}

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
