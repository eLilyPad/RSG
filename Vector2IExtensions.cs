using Godot;

namespace RSG;

public static class Vector2IExtensions
{
	public static IEnumerable<KeyValuePair<Vector2I, T>> InLine<T>(
		this IEnumerable<KeyValuePair<Vector2I, T>> value,
		int index,
		Func<Vector2I, int> indexer
	)
	{
		return value.Where(pair => indexer(pair.Key) == index);
	}
	public static IEnumerable<KeyValuePair<Vector2I, T>> AllInLines<T>(
		this IEnumerable<KeyValuePair<Vector2I, T>> tiles,
		Vector2I position
	)
	{
		return tiles.Where(pair => pair.Key.EitherEqual(position));
	}
}
