using Godot;
using RSG.UI.Nonogram;

namespace RSG.Nonogram;

using static Display;

public static class DisplayExtensions
{
	public static string AsText(
		this PenMode mode
	) => mode switch
	{
		PenMode.Block => BlockText,
		PenMode.Fill => FillText,
		_ => EmptyText
	};
	public static string FillButton(this PenMode mode, string current) => mode switch
	{
		PenMode.Block => current is EmptyText or FillText ? BlockText : EmptyText,
		PenMode.Fill => current is EmptyText ? FillText : EmptyText,
		_ => current
	};
	public static int IndexFrom(
		this Side side,
		Vector2I position
	) => side switch
	{
		Side.Column => position.Y,
		Side.Row => position.X,
		_ => -1
	};
	public static string AsFormat(this Side side) => side switch
	{
		Side.Column => "\n",
		Side.Row => "\t",
		_ => ""
	};
	public static string CalculateHints(
		this Dictionary<Vector2I, Button> buttons,
		HintPosition position
	) => buttons
		.Where(pair => position.Side.IndexFrom(position: pair.Key) == position.Index)
		.Select(pair => pair.Value.Text is FillText ? 1 : 0)
		.ToList()
		.Condense()
		.Where(i => i > 0)
		.Aggregate(EmptyHint, (current, i) => i > 0 && current is EmptyHint
			? position.Side.AsFormat() + i
			: current + position.Side.AsFormat() + i
		);
	public static void SetText(
		this Dictionary<HintPosition, RichTextLabel> hints,
		Func<HintPosition, string> asText,
		params Span<OneOf<HintPosition, Vector2I>> positions
	)
	{
		foreach (OneOf<HintPosition, Vector2I> position in positions)
		{
			hints.SetText(
				getText: asText,
				keys: position.Match(hint => [hint], vector => HintPosition.Convert(vector))
			);
		}
	}
	public static bool Matches(
		this IReadOnlyDictionary<Vector2I, bool> states,
		Dictionary<Vector2I, Button> tiles
	) => states
		.Select(selector: pair => tiles.GetValueOrDefault(pair.Key))
		.All(predicate: button => button?.Text == PenMode.Fill.AsText());
}
