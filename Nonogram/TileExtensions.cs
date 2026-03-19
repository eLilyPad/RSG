namespace RSG.Nonogram;

public static class TileExtensions
{
	public static T LockTiles<T>(this T values, bool locked) where T : IEnumerable<Tile>
	{
		foreach (Tile tile in values) tile.Locked = locked;
		return values;
	}
	public static IEnumerable<KeyValuePair<T, Tile>> HoverTiles<T>(this IEnumerable<KeyValuePair<T, Tile>> tiles, bool value)
	{
		foreach ((T _, Tile tile) in tiles)
		{
			tile.Hovering = value;
		}
		return tiles;
	}
}
