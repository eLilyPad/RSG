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
public interface IHandleEvents
{
	void Failed(Manager.Data data);
	void Completed(Manager.Data data);
}
sealed class UserInput
{
	public const MouseButton UnCoverButton = MouseButton.Left, FlagButton = MouseButton.Right;
	public required Tile.Pool Tiles { private get; init; }
	public required AutoCompleter Completer { private get; init; }

	public void MousePressed(Manager.Data data, Vector2I position, IHandleEvents? handler = null)
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
				handler?.Failed(data);
				break;
			case Tile.Mode.Empty when tile.Button.Text == string.Empty:
				Completer.FloodFillEmpty(data, position);
				break;
			default: break;
		}
		if (IsCompleted(data))
		{
			handler?.Completed(data);
		}
	}
	private bool IsCompleted(Manager.Data data)
	{
		foreach (var (position, (mode, _)) in data.State)
		{
			Tile tile = Tiles.GetOrCreate(position);
			bool tileCorrect = mode switch
			{
				Tile.Mode.Bomb when tile.Flagged || tile.Covered => true,
				Tile.Mode.Empty when !tile.Covered => true,
				_ => false,
			};
			if (!tileCorrect) return false;
		}
		return true;
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
			Data data = new() { State = state.ToImmutableDictionary() };
			return data;
		}
		public int Size => (int)Mathf.Sqrt(State.Count);
		public required IImmutableDictionary<Vector2I, (Tile.Mode mode, bool covered)> State { get; init; }
	}
	public Data Puzzle
	{
		private get; set => UI.PuzzleSize = (field = value).Size;
	} = Data.CreateRandom();
	public required MinesweeperContainer UI { get; init; }
	public IHandleEvents? EventHandler { get; set; }
	public bool IsCompleted => Puzzle.State.Values.All(v => v.covered is false || v.mode is Tile.Mode.Bomb);

	private AutoCompleter Completer => field ??= new AutoCompleter { Tiles = UI.Tiles };
	private UserInput Input => field ??= new UserInput { Tiles = UI.Tiles, Completer = Completer };

	public void OnActivate(Vector2I position, Tile tile)
	{
		Input.MousePressed(data: Puzzle, position, EventHandler);
	}
	public Tile.Mode GetType(Vector2I position)
	{
		(Tile.Mode mode, bool covered) defaultValue = (Tile.Mode.Empty, true);
		return Puzzle.State.GetValueOrDefault(position, defaultValue).mode;
	}
}

