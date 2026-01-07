using Godot;

namespace RSG.Nonogram;

using static Display;

public static class TileExtensions
{
	public static bool ShouldIgnore(this TileMode expected, TileMode current, TileMode newValue) =>
		expected == current
		&& !(newValue is TileMode.Blocked && current is TileMode.Clear);
	public static bool IsCorrect<TKey>(this IImmutableDictionary<TKey, TileMode> tiles, TKey position, TileMode current)
	{
		if (!tiles.TryGetValue(position, out TileMode expected)) return false;
		return current.IsCorrectMode(expected);
	}
	public static bool IsCorrectMode(this TileMode current, TileMode expected) => expected switch
	{
		TileMode.Filled when current is TileMode.Filled => true,
		TileMode.Clear when current is TileMode.Clear or TileMode.Blocked => true,
		_ => false
	};

	public static double ToDouble(this TileMode mode) => mode switch
	{
		TileMode.Blocked => 2,
		TileMode.Filled => 1,
		_ => 0,
	};
	public static TileMode Change(this TileMode input, TileMode currents) => input switch
	{
		TileMode.NULL => currents,
		TileMode mode when mode == currents => TileMode.Clear,
		TileMode mode => mode
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
		EmptyText => TileMode.Clear,
		_ => TileMode.NULL
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
