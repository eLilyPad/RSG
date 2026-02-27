using Godot;

namespace RSG.Nonogram;

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

		public HintPosition(Side side, Vector2I position) : this(side, side.IndexFrom(position)) { }
		public readonly (HorizontalAlignment, VerticalAlignment) Alignment() => (
			Side switch
			{
				Side.Row => HorizontalAlignment.Right,
				Side.Column => HorizontalAlignment.Center,
				_ => HorizontalAlignment.Fill
			},
			Side switch
			{
				Side.Row => VerticalAlignment.Center,
				Side.Column => VerticalAlignment.Bottom,
				_ => VerticalAlignment.Fill
			}
		);
	}
}
