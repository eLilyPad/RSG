using System.Text;
using Godot;

namespace RSG.Nonogram;

using static Display;

public static class DisplayExtensions
{
	//public static IEnumerable<int> AsLineHints(
	//	this IEnumerable<KeyValuePair<Vector2I, TileMode>> tiles,
	//	HintPosition position
	//)
	//{
	//	return tiles.CalculateHintsV2(position, selector: TileModeExtensions.IsFilled);
	//}
	//public static IOrderedEnumerable<KeyValuePair<Vector2I, T>> InLine<T>(
	//	this IEnumerable<KeyValuePair<Vector2I, T>> tiles,
	//	Vector2I position,
	//	Side side
	//)
	//{
	//	return tiles.InOrderedLine(position, Index, Order);
	//	int Index(Vector2I pos) => side.IndexFrom(position: pos);
	//	int Order(Vector2I pos) => side.OrderFrom(position: pos);
	//}
	//public static IEnumerable<int> CalculateHintsV2<TValue>(
	//	this IEnumerable<KeyValuePair<Vector2I, TValue>> tiles,
	//	HintPosition position,
	//	Func<TValue, bool> selector
	//)
	//{
	//	int connected = 0;
	//	foreach ((Vector2I pos, TValue value) in tiles
	//		.InLine(Index(position.Origin), Index)
	//		.OrderBy(Order)
	//	)
	//	{
	//		if (selector(value))
	//		{
	//			connected++;
	//			continue;
	//		}
	//		if (connected == 0) continue;
	//		yield return connected;
	//		connected = 0;
	//	}
	//	if (connected > 0) yield return connected;

	//	int Index(Vector2I pos) => position.Side.IndexFrom(position: pos);
	//	int Order(Vector2I pos) => position.Side.OrderFrom(position: pos);
	//}
	//private static string CalculateHints<TValue>(
	//	this IEnumerable<KeyValuePair<Vector2I, TValue>> tiles,
	//	HintPosition position,
	//	Func<TValue, int> selector
	//)
	//{
	//	StringBuilder builder = new();
	//	foreach (int value in tiles.CalculateHintsV2(position, selector))
	//	{
	//		builder
	//			.Append(value)
	//			.Append(value: position.Side.AsFormat());
	//	}

	//	return builder.Length > 0
	//		? builder.ToString()
	//		: EmptyHint + position.Side.AsFormat();
	//}
}
