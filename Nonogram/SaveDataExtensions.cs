using Godot;

namespace RSG.Nonogram;

using Mode = Display.TileMode;
using static Display;

public static class SaveDataExtensions
{
	public static bool TryGetIndex(
		this IDictionary<Vector2I, Mode> state,
		int[] hints,
		Side side,
		Vector2I position,
		[MaybeNullWhen(false)] out int index
	)
	{
		Assert(state.ContainsKey(position));
		Assert(state.IsSquare<IDictionary<Vector2I, Mode>, Mode>());

		index = default;
		Mode mode = state[position];
		if (mode is not Mode.Filled) return false;

		var a = state.InLine(position, side);

		return false;
	}
}
