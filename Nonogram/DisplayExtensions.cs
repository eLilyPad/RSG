using System.Text;
using Godot;

namespace RSG.Nonogram;

using static Display;

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
		.Where(pair => position.IndexFrom(pair.Key) == position.Index)
		.OrderBy(pair => position.OrderFrom(pair.Key));
	public static IEnumerable<KeyValuePair<Vector2I, T>> AllInLines<T>(
		this IEnumerable<KeyValuePair<Vector2I, T>> tiles,
		Vector2I position
	)
	{
		return tiles.Where(pair => pair.Key.EitherEqual(position));
	}
	public static string CalculateHints(this IImmutableDictionary<Vector2I, TileMode> tiles, HintPosition position)
	{
		return tiles.CalculateHints(position, selector: value => value is TileMode.Fill ? 1 : 0);
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

		foreach ((Vector2I coord, TValue? value) in tiles.OrderedLine(position))
		{
			if (selector(value) > 0) run++;
			else builder.FlushRun(position, ref run);
		}
		builder.FlushRun(position, ref run);
		return builder.Length switch
		{
			> 0 => builder.ToString(),
			_ => EmptyHint + position.AsFormat(),
		};
	}
	private static void FlushRun(this StringBuilder builder, HintPosition position, ref int run)
	{
		if (run <= 0) return;
		builder.Append(run);
		builder.Append(position.AsFormat());
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
			TileMode.Fill => filledTile,
			TileMode.Block => blocked,
			_ => background
		};
		button.AddThemeStyleboxOverride(themeName, style);
	}
	public static bool Matches(this Button button, TileMode state)
	{
		return (button.Text is FillText && state is TileMode.Fill)
			|| (button.Text is EmptyText && state is TileMode.Clear or TileMode.Block);
	}
	public static double ToDouble(this TileMode mode) => mode switch
	{
		TileMode.Block => 2,
		TileMode.Fill => 1,
		_ => 0,
	};
	public static TileMode ToTileMode(this int mode) => mode switch
	{
		2 => TileMode.Block,
		1 => TileMode.Fill,
		_ => 0,
	};
	public static TileMode FromText(this string mode) => mode switch
	{
		BlockText => TileMode.Block,
		FillText => TileMode.Fill,
		_ => TileMode.Clear
	};
	public static void PlayAudio(this TileMode mode)
	{
		if (mode.AsAudioStream() is AudioStream stream) Audio.Buses.SoundEffects.Play(stream);
	}
	public static AudioStream? AsAudioStream(this TileMode mode) => mode switch
	{
		TileMode.Fill => Audio.NonogramSounds.FillTileClicked,
		TileMode.Block => Audio.NonogramSounds.BlockTileClicked,
		_ => null
	};
	public static string AsText<T>(this IImmutableDictionary<T, TileMode> modes, T position) where T : notnull
	{
		return modes.GetValueOrDefault(position, TileMode.Clear).AsText();
	}
	public static string AsText(this TileMode mode) => mode switch
	{
		TileMode.Block => BlockText,
		TileMode.Fill => FillText,
		_ => EmptyText,
	};
}
