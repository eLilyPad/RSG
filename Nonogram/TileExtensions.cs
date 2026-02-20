namespace RSG.Nonogram;

using static Display;

public static class TileExtensions
{
	public static bool IsCorrect<TKey>(
		this IImmutableDictionary<TKey, TileMode> tiles,
		TKey position,
		TileMode current
	)
	{
		if (!tiles.TryGetValue(position, out TileMode expected)) return false;
		return current.IsCorrectMode(expected);
	}
}
