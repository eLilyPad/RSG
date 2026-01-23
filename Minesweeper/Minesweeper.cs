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
				bool isBomb = Random.Shared.Next(10) == 0;
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
	public required IHandleEvents EventHandler { get; set; }
	public bool IsCompleted => UI.Tiles.AllEmptyUnCovered();

	private AutoCompleter Completer => field ??= new AutoCompleter { Tiles = UI.Tiles };
	private UserInput Input => field ??= new UserInput { Tiles = UI.Tiles };

	public void OnActivate(Vector2I position, Tile tile)
	{
		var inputResponse = Input.MousePressed(position);
		inputResponse.Switch(
			flag => { },
			uncovered => TileUncovered(uncovered, position),
			nothing => { }
		);
	}

	public Tile.Mode GetType(Vector2I position)
	{
		(Tile.Mode mode, bool covered) defaultValue = (Tile.Mode.Empty, true);
		return Puzzle.State.GetValueOrDefault(position, defaultValue).mode;
	}

	private void TileUncovered(UserInput.UnCovered uncovered, Vector2I position)
	{
		Data data = Puzzle;
		switch (uncovered.Type)
		{
			case Tile.Mode.Bomb:
				GD.Print("BOOM!");
				EventHandler.Failed(data);
				return;
			case Tile.Mode.Empty:
				Completer.FloodFillEmpty(data, start: position);
				if (IsCompleted)
				{
					EventHandler.Completed(data);
					return;
				}
				break;
		}
	}
}

