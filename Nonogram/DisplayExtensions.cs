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
	public static string CalculateHints(
		this IEnumerable<KeyValuePair<Vector2I, TileMode>> tiles,
		HintPosition position
	) => tiles.CalculateHints(position, selector: value => value is TileMode.Filled ? 1 : 0);
	public static IOrderedEnumerable<KeyValuePair<Vector2I, T>> InLine<T>(
		this IEnumerable<KeyValuePair<Vector2I, T>> tiles,
		Vector2I position,
		Side side
	)
	{
		return tiles
			.InLine(index: Index(position), indexer: Index)
			.OrderBy(keySelector: pair => side.OrderFrom(position: pair.Key));

		int Index(Vector2I pos) => side.IndexFrom(position: pos);
	}
	private static string CalculateHints<TValue>(
		this IEnumerable<KeyValuePair<Vector2I, TValue>> tiles,
		HintPosition position,
		Func<TValue, int> selector
	)
	{
		StringBuilder builder = new();
		int run = 0;
		var line = tiles
			.InLine(index: position.Index, indexer: pos => position.Side.IndexFrom(position: pos))
			.OrderBy(keySelector: pair => position.Side.OrderFrom(position: pair.Key));

		foreach ((Vector2I _, TValue? value) in line)
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
			: EmptyHint + position.Side.AsFormat();
	}
	private static void FlushRun(this StringBuilder builder, Side side, ref int run)
	{
		if (run <= 0) return;
		builder.Append(run);
		builder.Append(side.AsFormat());
		run = 0;
	}
}
