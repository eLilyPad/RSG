using Godot;

namespace RSG.Nonogram;

using static Display;

public static class DisplayExtensions
{
	public static string CalculateHints(this Dictionary<Vector2I, Tile> buttons, HintPosition position)
	{
		return buttons
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

	public static bool Matches(this Button button, bool state)
	{
		return (button.Text is FillText && state) || (button.Text is EmptyText && !state);
	}

}
