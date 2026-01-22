using Godot;

namespace RSG.Minesweeper;

sealed class AutoCompleter
{
	public required Tile.Pool Tiles { private get; init; }

	public void FloodFillEmpty(Manager.Data data, Vector2I start)
	{
		IImmutableDictionary<Vector2I, (Tile.Mode mode, bool covered)> saved = data.State;
		HashSet<Vector2I> visited = [start];
		Queue<Vector2I> queue = new();
		queue.Enqueue(start);
		int maxIterations = saved.Count, i = 0;
		while (queue.Count > 0)
		{
			if (i++ >= maxIterations) break;
			Vector2I current = queue.Dequeue();

			foreach (Vector2I next in saved.Keys.PointsAround(current))
			{
				Tile tile = Tiles.GetOrCreate(next);
				if (tile.Type is not Tile.Mode.Empty) { continue; }
				if (visited.Contains(next)) { continue; }
				tile.Covered = false;
				visited.Add(next);
				if (tile.Button.Text == string.Empty) queue.Enqueue(next);
			}
		}
	}
}

