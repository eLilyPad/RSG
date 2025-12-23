using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;
using static Nonogram.PuzzleManager;
using static Nonogram.NonogramContainer;

public static class CoreInitializer
{
	private static class Errors
	{
		public const string NoDisplayGiven = "No Displays given, provide at least one display when creating the Container";
		public const string OutsideOfTree = "Outside of Scene Tree unable to initialize";
	}
}
