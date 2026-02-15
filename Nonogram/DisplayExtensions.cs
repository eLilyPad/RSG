using System.Text;
using Godot;

namespace RSG.Nonogram;

using static Display;

public static class HintExtensions
{
	public static string AsFormat(this Side side) => side switch
	{
		Side.Column => "\n",
		Side.Row => " ",
		_ => ""
	};
	public static int IndexFrom(this Side side, Vector2I position) => side switch
	{
		Side.Column => position.Y,
		Side.Row => position.X,
		_ => throw new ArgumentOutOfRangeException(nameof(position))
	};
	public static int OrderFrom(this Side side, Vector2I position) => side switch
	{
		Side.Column => position.X,
		Side.Row => position.Y,
		_ => throw new ArgumentOutOfRangeException(nameof(position))
	};
}
public static class DisplayExtensions
{
	public static string AsName(this PuzzleManager.Type type) => type switch
	{
		PuzzleManager.Type.Game => "Game",
		PuzzleManager.Type.Paint => "Paint",
		_ => "Puzzle Display"
	};

	public static IOrderedEnumerable<KeyValuePair<Vector2I, T>> OrderedLine<T>(
		this IEnumerable<KeyValuePair<Vector2I, T>> tiles,
		HintPosition position
	) => tiles
		.Where(pair => position.Side.IndexFrom(pair.Key) == position.Index)
		.OrderBy(pair => position.Side.OrderFrom(pair.Key));
	public static IEnumerable<KeyValuePair<Vector2I, T>> AllInLines<T>(
		this IEnumerable<KeyValuePair<Vector2I, T>> tiles,
		Vector2I position
	) => tiles.Where(pair => pair.Key.EitherEqual(position));
	public static IOrderedEnumerable<KeyValuePair<Vector2I, T>> InLine<T>(
		this IEnumerable<KeyValuePair<Vector2I, T>> tiles,
		Vector2I position,
		Side side
	) => tiles
		.Where(pair => side.IndexFrom(pair.Key) == side.IndexFrom(position))
		.OrderBy(pair => side.OrderFrom(pair.Key));
	public static string CalculateHints(this IEnumerable<KeyValuePair<Vector2I, TileMode>> tiles, HintPosition position)
	{
		return tiles.CalculateHints(position, selector);
		static int selector(TileMode value) => value is TileMode.Filled ? 1 : 0;

	}
	private static string CalculateHints<TValue>(
		this IEnumerable<KeyValuePair<Vector2I, TValue>> tiles,
		HintPosition position,
		Func<TValue, int> selector
	)
	{
		StringBuilder builder = new();
		int run = 0;

		foreach ((Vector2I _, TValue? value) in tiles.OrderedLine(position))
		{
			if (selector(value) > 0)
			{
				run++;
				continue;
			}
			builder.FlushRun(position.Side, ref run);
		}
		builder.FlushRun(position.Side, ref run);
		return builder.Length > 0
			? builder.ToString()
			: Hint.Empty + position.Side.AsFormat();
	}
	private static void FlushRun(this StringBuilder builder, Side side, ref int run)
	{
		if (run <= 0) return;
		builder.Append(run);
		builder.Append(side.AsFormat());
		run = 0;
	}
}
