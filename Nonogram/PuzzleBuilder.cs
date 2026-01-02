using Godot;

namespace RSG.Nonogram;

public static class PuzzleBuilder
{
	public enum Shape { Circle, Diamond }

	public static bool IsOverNoiseThreshold(this Vector2I position, float threshold)
	{
		(int x, int y) = position;
		FastNoiseLite noise = new()
		{
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Frequency = 0.1f,
			Seed = 1
		};
		return noise.GetNoise2D(x, y) > threshold;
	}
	public static bool IsInLines(this Vector2I position, int thickness = 1, params ReadOnlySpan<(Vector2I start, Vector2I stop)> values)
	{
		foreach ((Vector2I start, Vector2I stop) in values)
		{
			if (start == stop)
			{
				return position.DistanceTo(start) <= thickness * .5f;
			}
			Vector2
			pos = position,
			ab = stop - start,
			ap = position - start;

			float t = ap.Dot(ab) / ab.LengthSquared();
			t = Mathf.Clamp(t, 0, 1);

			Vector2 closest = start + ab * t;
			float maxDistance = thickness * 0.5f;
			if (pos.DistanceTo(closest) <= maxDistance) return true;
		}
		return false;
	}
	public static bool IsInSpiral(this Vector2I position, Vector2I center, int a, int b, int thickness = 0)
	{
		Vector2I offset = center - position;
		return Mathf.Abs(offset.Squared() - (a + b * Mathf.Atan2(offset.Y, offset.X))) <= thickness;
	}
	public static bool IsInCircle(this Vector2I position, Vector2I center, int radius, int thickness = 0)
	{
		Vector2I offset = center - position;
		int
		innerRadius = radius - thickness,
		offsetSquared = offset.Squared();
		return thickness < 0 ? In(radius) : In(radius) && !In(innerRadius);

		bool In(int size) => size * size >= offsetSquared;
	}
	public static bool IsIn(this Vector2I position, Vector2I center, int radius, Shape shape, int thickness = 0)
	{
		Vector2I offset = center - position;
		return shape switch
		{
			Shape.Circle when thickness > 0 => InCircle(radius) && !InCircle(radius - thickness),
			Shape.Circle => InCircle(radius),
			Shape.Diamond when thickness > 0 => InDiamond(radius) && !InDiamond(radius - thickness),
			Shape.Diamond => InDiamond(radius),
			_ => false
		};

		bool InCircle(int size) => size * size >= offset.Squared();
		bool InDiamond(int size) => Mathf.Abs(offset.X) + Mathf.Abs(offset.Y) <= size;
	}
}
