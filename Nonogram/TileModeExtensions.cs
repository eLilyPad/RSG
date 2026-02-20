using Godot;

namespace RSG.Nonogram;

using static Display;

public static class TileModeExtensions
{
	public static bool IsCorrectMode(this TileMode current, TileMode expected) => expected switch
	{
		TileMode.Filled when current is TileMode.Filled => true,
		TileMode.Clear when current is TileMode.Clear or TileMode.Blocked => true,
		_ => false
	};
	public static bool IsValidInput(this TileMode current, ref TileMode input)
	{
		if (input is TileMode.NULL) return false;
		input = input == current ? TileMode.Clear : input;
		return !TileMode.Clear.AllEqual(current, input);
	}
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
}
