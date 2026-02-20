namespace RSG.Nonogram;

public static class TileExtensions
{
	public static IEnumerable<KeyValuePair<T, Tile>> HoverTiles<T>(this IEnumerable<KeyValuePair<T, Tile>> tiles, bool value)
	{
		foreach ((T _, Tile tile) in tiles)
		{
			tile.Hovering = value;
		}
		return tiles;
	}
}
