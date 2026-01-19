using Godot;

namespace RSG.Minesweeper;

public interface IColours
{
	Color MinesweeperBombBackground { get; }
	Color MinesweeperEmptyBackground { get; }
	Color MinesweeperCoveredBackground { get; }
	Color MinesweeperBackground { get; }

	Color MineSweeperBackground(Tile.Mode mode, bool covered)
	{
		if (covered) return MinesweeperCoveredBackground;
		return mode switch
		{
			Tile.Mode.Bomb => MinesweeperBombBackground,
			_ => MinesweeperEmptyBackground
		};
	}
}
sealed class UserInput
{
	public const MouseButton UnCoverButton = MouseButton.Left, FlagButton = MouseButton.Right;
	public required Tile.Pool Tiles { private get; init; }
	public required AutoCompleter Completer { private get; init; }

	public void MousePressed(Manager.Data data, Vector2I position)
	{
		Tile tile = Tiles.GetOrCreate(position);
		if (Input.IsMouseButtonPressed(FlagButton))
		{
			tile.Flagged = !tile.Flagged;
			return;
		}
		if (tile.Flagged) return;
		tile.Covered = false;
		switch (tile.Type)
		{
			case Tile.Mode.Bomb:
				GD.Print("Boom! You hit a mine!");
				break;
			case Tile.Mode.Empty when tile.Button.Text == string.Empty:
				Completer.FloodFillEmpty(data, position);
				break;
			default: break;
		}
	}
}
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
public sealed partial class Manager : Tile.IProvider
{
	public sealed class Data
	{
		public static Data CreateRandom(int size = 5)
		{
			Dictionary<Vector2I, (Tile.Mode mode, bool covered)> state = [];
			foreach (Vector2I position in (size * Vector2I.One).GridRange())
			{
				Tile.Mode mode = Random.Shared.Next(10) > 1 ? Tile.Mode.Empty : Tile.Mode.Bomb;
				bool covered = true;
				state[position] = (mode, covered);
			}
			Data data = new(state: state);
			return data;
		}
		public int Size => (int)Mathf.Sqrt(_state.Count);
		public IImmutableDictionary<Vector2I, (Tile.Mode mode, bool covered)> State => _state.ToImmutableDictionary();
		private readonly Dictionary<Vector2I, (Tile.Mode mode, bool covered)> _state = [];

		private Data(Dictionary<Vector2I, (Tile.Mode mode, bool covered)> state) => _state = state;
	}
	public Data Puzzle
	{
		private get; set => UI.PuzzleSize = (field = value).Size;
	} = Data.CreateRandom();
	public required MinesweeperContainer UI { get; init; }

	private AutoCompleter Completer => field ??= new AutoCompleter { Tiles = UI.Tiles };
	private UserInput Input => field ??= new UserInput { Tiles = UI.Tiles, Completer = Completer };

	public void OnActivate(Vector2I position, Tile tile)
	{
		Input.MousePressed(data: Puzzle, position);
	}
	public Tile.Mode GetType(Vector2I position)
	{
		(Tile.Mode mode, bool covered) defaultValue = (Tile.Mode.Empty, true);
		return Puzzle.State.GetValueOrDefault(position, defaultValue).mode;
	}
}

