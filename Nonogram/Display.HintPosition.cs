using Godot;

namespace RSG.Nonogram;

using static Display;


public static class HintExtensions
{
	public static string AsFormat(this Side side) => side switch
	{
		Side.Column => "\n",
		Side.Row => " ",
		_ => ""
	};
	public static int IndexFrom(this Side side, Vector2I position) => side switch
	{
		Side.Column => position.Y,
		Side.Row => position.X,
		_ => throw new ArgumentOutOfRangeException(nameof(position))
	};
	public static int OrderFrom(this Side side, Vector2I position) => side switch
	{
		Side.Column => position.X,
		Side.Row => position.Y,
		_ => throw new ArgumentOutOfRangeException(nameof(position))
	};
	public static Vector2I Origin(this Side side)
	{
		int s = (int)side;
		Assert(s is 0 or 1);
		return new(s, s ^ 1);
	}
	public static void Shift(this Side side, ref Vector2I position, int amount)
	{
		int s = (int)side;
		Assert(s is 0 or 1);
		position.X += (s ^ 1) * amount;
		position.Y += s * amount;
	}
	public static HorizontalAlignment AsHAlignment(this Side side) => side switch
	{
		Side.Row => HorizontalAlignment.Right,
		Side.Column => HorizontalAlignment.Center,
		_ => HorizontalAlignment.Fill
	};
	public static VerticalAlignment AsVAlignment(this Side side) => side switch
	{
		Side.Row => VerticalAlignment.Center,
		Side.Column => VerticalAlignment.Bottom,
		_ => VerticalAlignment.Fill
	};
}
public abstract partial class Display
{
	public readonly record struct HintPosition(Side Side, int Index)
	{
		public static implicit operator Side(HintPosition hint) => hint.Side;
		public static implicit operator int(HintPosition hint) => hint.Index;
		public static IEnumerable<HintPosition> AsRange(int length, int start = 0) => Range(start, count: length)
			.SelectMany(i => Convert(i));
		public static IEnumerable<HintPosition> Convert(OneOf<Vector2I, int> value) => [
			new(Side.Row, value.Match(position => position.X, index => index)),
			new(Side.Column, value.Match(position => position.Y, index => index))
		];
		public static (HintPosition Row, HintPosition Column) ConvertVector(Vector2I value)
		{
			return (new(Side.Row, value.X), new(Side.Column, value.Y));
		}

		public readonly Vector2I Origin = Side.Origin() * Index;
		public readonly string Format = Side.AsFormat();

		public HintPosition(Side side, Vector2I position) : this(side, side.IndexFrom(position)) { }
		public readonly (HorizontalAlignment, VerticalAlignment) Alignment() => (Side.AsHAlignment(), Side.AsVAlignment());
		public readonly int IndexFrom(Vector2I position) => Side.IndexFrom(position);
		public readonly int OrderFrom(Vector2I position) => Side.IndexFrom(position);
	}
}
