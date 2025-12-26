using Godot;

namespace RSG.Nonogram;

using static Display;

public static class DisplayExtensions
{
	public static string CalculateHints(this Dictionary<Vector2I, Tile> tiles, HintPosition position)
	{
		return tiles
			.Where(pair => position.IndexFrom(position: pair.Key) == position.Index)
			.Select(pair => pair.Value.Button.Text is FillText ? 1 : 0)
			.ToList()
			.Condense()
			.Where(i => i > 0)
			.Aggregate(EmptyHint, (current, i) => i > 0 && current is EmptyHint
				? position.AsFormat() + i
				: current + position.AsFormat() + i
			);
	}
	public static IEnumerable<Vector2I> AllInLines<T>(this Dictionary<Vector2I, T> tiles, Vector2I position)
	{
		return tiles
			.Where(pair => pair.Key.X == position.X || pair.Key.Y == position.Y)
			.Select(pair => pair.Key);
	}
}
public static class TileExtensions
{
	public static bool Matches(this Button button, TileMode state)
	{
		return (button.Text is FillText && state is TileMode.Fill)
			|| (button.Text is EmptyText && state is TileMode.Clear or TileMode.Block);
	}
	public static double ToDouble(this TileMode mode)
	{
		return mode switch
		{
			TileMode.Block => 2,
			TileMode.Fill => 1,
			_ => 0,
		};
	}
	public static TileMode ToTileMode(this int mode)
	{
		return mode switch
		{
			2 => TileMode.Block,
			1 => TileMode.Fill,
			_ => 0,
		};
	}
	public static TileMode FromText(this string mode)
	{
		return mode switch
		{
			BlockText => TileMode.Block,
			FillText => TileMode.Fill,
			_ => TileMode.Clear
		};
	}
	public static string AsText<T>(this IImmutableDictionary<T, TileMode> modes, T position) where T : notnull
	{
		return modes.GetValueOrDefault(position, TileMode.Clear).AsText();
	}
	public static string AsText(this TileMode mode)
	{
		return mode switch
		{
			TileMode.Block => BlockText,
			TileMode.Fill => FillText,
			_ => EmptyText,
		};
	}
}
