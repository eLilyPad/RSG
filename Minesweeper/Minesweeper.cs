using Godot;

namespace RSG.Minesweeper;

public interface IHandleEvents
{
	void Failed(Manager.Data data);
	void Completed(Manager.Data data);
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
				bool isBomb = Random.Shared.Next(10) > 1;
				Tile.Mode mode = isBomb ? Tile.Mode.Bomb : Tile.Mode.Empty;
				state[position] = (mode, true);
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
	public bool IsCompleted
	{
		get
		{
			foreach (var (position, (mode, _)) in Puzzle.State)
			{
				Tile tile = UI.Tiles.GetOrCreate(position);
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

	private AutoCompleter Completer => field ??= new AutoCompleter { Tiles = UI.Tiles };
	private UserInput Input => field ??= new UserInput { Tiles = UI.Tiles };

	public void OnActivate(Vector2I position, Tile tile)
	{
		var inputResponse = Input.MousePressed(position);
		inputResponse.Switch(
			flag => { },
			uncovered =>
			{
				switch (uncovered.Type)
				{
					case Tile.Mode.Bomb:
						EventHandler?.Failed(Puzzle);
						return;
					case Tile.Mode.Empty:
						if (IsCompleted)
						{
							EventHandler?.Completed(Puzzle);
							return;
						}
						Completer.FloodFillEmpty(Puzzle, position);
						break;
				}
			});
	}
	public Tile.Mode GetType(Vector2I position)
	{
		(Tile.Mode mode, bool covered) defaultValue = (Tile.Mode.Empty, true);
		return Puzzle.State.GetValueOrDefault(position, defaultValue).mode;
	}
}

