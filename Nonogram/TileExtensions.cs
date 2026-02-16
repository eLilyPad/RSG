using Godot;

namespace RSG.Nonogram;

using static Display;

public interface ITiles<T> { NodePool<Vector2I, Tile, T> Tiles { get; } }
public static class TileExtensions
{
	const int UnlockedWidth = 0, LockedWidth = 2;
	public static bool TryLock<T>(this T config, Vector2I position)
	where T : ITiles<T>, Tile.ILocker, IDisplayType
	{
		Tile tile = config.Tiles.GetOrCreate(position, config);
		bool shouldLock = config.ShouldLock(position);
		if (!shouldLock) return false;
		tile.Button.SetLocked(locked: shouldLock);
		return true;
	}
	public static bool TryGetLocked<TConfig>(this TConfig config, Vector2I position)
	where TConfig : ITiles<TConfig>, SaveData.IHave
	{
		config.Tiles.GetOrCreate(position, config).Button.IsLocked(out bool locked);
		return locked;
	}
	public static Button IsLocked(this Button tile, out bool locked)
	{
		int width = -1;
		tile.OverrideStyle((StyleBoxFlat style) =>
		{
			width = style.GetBorderWidthMin();
			return style;
		});
		locked = width is LockedWidth;
		return tile;
	}
	public static Button SetLocked(this Button tile, bool locked)
	{
		tile.OverrideStyle((StyleBoxFlat style) =>
		{
			style.SetBorderWidthAll(locked ? LockedWidth : UnlockedWidth);
			return style;
		});
		return tile;
	}

	public static bool TryGetMode<TConfig>(this TConfig config, Vector2I position, out TileMode value)
	where TConfig : ITiles<TConfig>, SaveData.IHave
	{
		value = TileMode.NULL;
		TileMode expectedMode = config.Puzzle.States.GetValueOrDefault(position);
		Tile tile = config.Tiles.GetOrCreate(key: position, config);
		IColours colours = Core.DefaultColours;
		Color expectedColour = colours.NonogramTileBackground(expectedMode, false);
		Color expectedColourOther = colours.NonogramTileBackground(expectedMode, true);
		Color currentColour = Colors.Transparent;
		tile.OverrideStyle((StyleBoxFlat style) =>
		{
			currentColour = style.BgColor;
			return style;
		});
		bool isSameMode = expectedColour == currentColour || expectedColourOther == currentColour;
		if (isSameMode)
		{
			value = expectedMode;
			return true;
		}
		return false;
	}

	public static TConfig SetTileHovering<TConfig>(this TConfig config, Vector2I position, bool value)
	where TConfig : ITiles<TConfig>
	{
		IEnumerable<KeyValuePair<Vector2I, Tile>> tiles = config.Tiles.AllInLines(position);
		foreach ((Vector2I _, Tile tile) in tiles)
		{
			tile.Button.Scale = Vector2.One * (value ? .9f : 1);
		}
		return config;
	}

	public static T SetDisplay<T>(this T config, Vector2I position)
	where T : SaveData.IHave, Tile.ILocker, IAlternate, ITiles<T>
	{
		Tile tile = config.Tiles.GetOrCreate(position, config);
		TileMode mode = config.Puzzle.States.GetValueOrDefault(position);
		(int x, int y) = position;
		tile.Name = $"Tile (X: {x}, Y: {y})";
		bool isAlternative = config.IsAlternative(position);
		tile.SetColours(mode, isAlternative, Core.DefaultColours);
		tile.Button.SetLocked(locked: config.ShouldLock(position));
		return config;
	}
}
