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
	public static bool IsValidInput(this Mode current, ref Mode input)
	{
		if (input is Mode.NULL) return false;
		input = input == current ? Mode.Clear : input;
		return !Mode.Clear.AllEqual(current, input);
	}
	public static void PlayAudio(this Mode mode)
	{
		if (mode.AsAudioStream() is AudioStream stream) Audio.Buses.SoundEffects.Play(stream);
	}
	public static AudioStream? AsAudioStream(this Mode mode) => mode switch
	{
		Mode.Filled => Audio.NonogramSounds.FillTileClicked,
		Mode.Blocked => Audio.NonogramSounds.BlockTileClicked,
		_ => null
	};
}
