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
	public static string AsName(this Type type) => type switch
	{
		Type.Game => "Game",
		Type.Paint => "Paint",
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
	)
	{
		return tiles.Where(pair => pair.Key.EitherEqual(position));
	}
	public static IOrderedEnumerable<KeyValuePair<Vector2I, T>> AllInLine<T>(
		this IEnumerable<KeyValuePair<Vector2I, T>> tiles,
		Vector2I position,
		Side side
	)
	{
		return tiles
			.Where(pair => side.IndexFrom(pair.Key) == side.IndexFrom(position))
			.OrderBy(pair => side.OrderFrom(pair.Key));
	}
	public static string CalculateHints(this IImmutableDictionary<Vector2I, TileMode> tiles, HintPosition position)
	{
		return tiles.CalculateHints(position, selector: value => value is TileMode.Filled ? 1 : 0);
	}
	public static string CalculateHints(this Dictionary<Vector2I, Tile> tiles, HintPosition position)
	{
		return tiles.CalculateHints(position, selector: value => value.Button.Text is FillText ? 1 : 0);
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
public static class TileExtensions
{
	public static void StyleTileBackground(
		this Button button,
		Vector2I position,
		IColours colours,
		StyleBoxFlat? style = null,
		TileMode? mode = null
	)
	{
		const int chunkSize = 5;
		const string themeName = "normal";
		style ??= button.GetThemeStylebox(themeName).Duplicate() as StyleBoxFlat;
		if (style is null) return;
		int chunkIndex = position.X / chunkSize + position.Y / chunkSize;
		Color filledTile = colours.NonogramTileBackgroundFilled;
		Color background = chunkIndex % 2 == 0 ? colours.NonogramTileBackground1 : colours.NonogramTileBackground2;
		Color blocked = background.Darkened(.4f);
		style.BgColor = mode switch
		{
			TileMode.Filled => filledTile,
			TileMode.Blocked => blocked,
			_ => background
		};
		button.AddThemeStyleboxOverride(themeName, style);
	}
	public static bool Matches(this Button button, TileMode state)
	{
		return (button.Text is FillText && state is TileMode.Filled)
			|| (button.Text is EmptyText && state is TileMode.Clear or TileMode.Blocked);
	}
	public static double ToDouble(this TileMode mode) => mode switch
	{
		TileMode.Blocked => 2,
		TileMode.Filled => 1,
		_ => 0,
	};
	public static TileMode ToTileMode(this int mode) => mode switch
	{
		2 => TileMode.Blocked,
		1 => TileMode.Filled,
		_ => 0,
	};
	public static TileMode FromText(this string mode) => mode switch
	{
		BlockText => TileMode.Blocked,
		FillText => TileMode.Filled,
		_ => TileMode.Clear
	};
	public static void PlayAudio(this TileMode mode)
	{
		if (mode.AsAudioStream() is AudioStream stream) Audio.Buses.SoundEffects.Play(stream);
	}
	public static AudioStream? AsAudioStream(this TileMode mode) => mode switch
	{
		TileMode.Filled => Audio.NonogramSounds.FillTileClicked,
		TileMode.Blocked => Audio.NonogramSounds.BlockTileClicked,
		_ => null
	};
	public static string AsText<T>(this IImmutableDictionary<T, TileMode> modes, T position) where T : notnull
	{
		return modes.GetValueOrDefault(position, TileMode.Clear).AsText();
	}
	public static string AsText(this TileMode mode) => mode switch
	{
		TileMode.Blocked => BlockText,
		TileMode.Filled => FillText,
		_ => EmptyText,
	};
}
