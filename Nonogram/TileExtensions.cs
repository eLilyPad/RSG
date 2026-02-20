namespace RSG.Nonogram;

using static Display;

public static class TileExtensions
{
	public static bool IsCorrect<T, TKey>(this T tiles, TKey position, TileMode current)
	where T : IImmutableDictionary<TKey, TileMode>
	{
		if (!tiles.TryGetValue(position, out TileMode expected)) return false;
		return current.IsCorrectMode(expected);
	}
}
