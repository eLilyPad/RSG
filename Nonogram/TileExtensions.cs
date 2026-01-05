using Godot;

namespace RSG.Nonogram;

using static Display;

public static class TileExtensions
{
	public static bool ShouldIgnore(this TileMode expected, TileMode current, TileMode newValue) =>
		expected == current
		&& !(newValue is TileMode.Blocked && current is TileMode.Clear);
	public static bool Matches(this TileMode current, TileMode expected) =>
		expected == current
		|| (expected == TileMode.Clear && current == TileMode.Blocked);

	public static double ToDouble(this TileMode mode) => mode switch
	{
		TileMode.Blocked => 2,
		TileMode.Filled => 1,
		_ => 0,
	};
	public static TileMode Change(this TileMode input, TileMode currents) => input switch
	{
		TileMode.NULL => input,
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
