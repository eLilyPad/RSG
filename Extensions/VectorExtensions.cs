using Godot;

namespace RSG.Extensions;

public static class VectorExtensions
{
	public static bool EitherEqual(this Vector2I position, Vector2I other) => other.X == position.X || other.Y == position.Y;
	public static int Squared(this Vector2I value) => value.X * value.X + value.Y * value.Y;
	public static IEnumerable<Vector2I> FloodGet(this IEnumerable<Vector2I> values, Vector2I start, Func<Vector2I, bool> connected)
	{
		HashSet<Vector2I> visited = [];
		Queue<Vector2I> queue = new();

		queue.Enqueue(start);
		visited.Add(start);

		while (queue.Count > 0)
		{
			Vector2I current = queue.Dequeue();

			foreach (Vector2I next in values.PointsAround(current))
			{
				if (visited.Contains(next) || !connected(next)) { continue; }
				visited.Add(next);
				queue.Enqueue(next);
			}
		}

		return visited;
	}
	public static IEnumerable<Vector2I> PointsAround(this IEnumerable<Vector2I> values, Vector2I position, int radius = 1)
	{
		Vector2I start = new(position.X - radius, position.Y - radius);
		for (int x = start.X; x < start.X + (radius * 2) + 1; x++)
		{
			for (int y = start.Y; y < start.Y + (radius * 2) + 1; y++)
			{
				Vector2I current = new(x, y);
				if (position == current || !values.Contains(current)) { continue; }
				yield return current;
			}
		}
	}
	public static IEnumerable<Vector2I> GridRange(this Vector2I size, Vector2I? startAt = null)
	{
		if (startAt is not Vector2I start) { start = Vector2I.Zero; }
		for (int x = start.X; x < size.X; x++)
		{
			for (int y = start.Y; y < size.Y; y++)
			{
				yield return new Vector2I(x, y);
			}
		}
	}
	public static bool IsInBorder(this Vector2I size, Vector2I position, int thickness = 1)
	{
		if (thickness <= 0) return false;

		// Optional safety check
		if (position.X < 0 || position.Y < 0 || position.X >= size.X || position.Y >= size.Y) return false;

		return position.X < thickness
			|| position.Y < thickness
			|| position.X >= size.X - thickness
			|| position.Y >= size.Y - thickness;
	}
	public static bool IsCorner(this Vector2I size, Vector2I position)
	{
		return (position.X == 0 && position.Y == 0)
			|| (position.X == size.X - 1 && position.Y == 0)
			|| (position.X == 0 && position.Y == size.Y - 1)
			|| (position.X == size.X - 1 && position.Y == size.Y - 1);
	}
	public static bool TryParse(this string? value, out Vector2I result)
	{
		result = Vector2I.Zero;
		if (string.IsNullOrEmpty(value)) { return false; }
		foreach ((int index, string part) in value.Trim('(', ')').Split(',').Index())
		{
			if (!int.TryParse(part, out int number))
			{
				GD.PrintErr($"Error parsing int from string part: {part}");
				return false;
			}
			switch (index)
			{
				case 0: result.X = number; break;
				case 1: result.Y = number; break;
			}
		}
		return true;
	}
}
