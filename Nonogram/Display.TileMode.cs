using System.Text.Json.Serialization;
using Godot;

namespace RSG.Nonogram;

using Mode = Display.TileMode;

public static class TileModeExtensions
{
	public static bool IsCorrect<TKey>(this IImmutableDictionary<TKey, Mode> tiles, TKey position, Mode current)
	{
		if (!tiles.TryGetValue(position, out Mode expected)) return false;
		return current.IsCorrectMode(expected);
	}
	public static bool IsCorrectMode(this Mode current, Mode expected) => expected switch
	{
		Mode.Filled when current is Mode.Filled => true,
		Mode.Clear when current is Mode.Clear or Mode.Blocked => true,
		_ => false
	};
	public static Mode ToClearWhenSame(this Mode current, Mode value) => value == current ? Mode.Clear : value;
	public static Mode ToClearWhenSame(this Mode current, ref Mode value) => value = current.ToClearWhenSame(value);
	public static bool IsValidInput(this Mode current, ref Mode input)
	{
		if (input is Mode.Clear) return false;
		current.ToClearWhenSame(ref input);
		return !Mode.Clear.AllEqual(current, input);
	}
	public static int AsBinaryMode(this Mode mode) => mode.IsFilled() ? 1 : 0;
	public static bool IsFilled(this Mode mode) => mode is Mode.Filled;
	public static Mode PlayAudio(this Mode mode)
	{
		if (mode.AsAudioStream() is AudioStream stream) Audio.Buses.SoundEffects.Play(stream);
		return mode;
	}
	public static AudioStream? AsAudioStream(this Mode mode) => mode switch
	{
		Mode.Filled => Audio.NonogramSounds.FillTileClicked,
		Mode.Blocked => Audio.NonogramSounds.BlockTileClicked,
		_ => null
	};
}

public abstract partial class Display
{
	[JsonConverter(typeof(JsonStringEnumConverter<Mode>))]
	public enum TileMode : ulong
	{
		Clear = 0UL,
		Filled = 1,
		Blocked = 2
	}

}