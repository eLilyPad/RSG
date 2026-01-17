using Godot;

namespace RSG.Minesweeper;

public interface IColours
{
	Color MinesweeperBombBackground { get; }
	Color MinesweeperEmptyBackground { get; }
	Color MinesweeperCoveredBackground { get; }

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
public sealed partial class Minesweeper : Tile.IProvider
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
		get; set
		{
			UI.PuzzleSize = value.Size;
			field = value;
		}
	} = Data.CreateRandom();
	public required MinesweeperContainer UI { get; init; }

	public void OnActivate(Vector2I position, Tile tile) => tile.Covered = false;
	public Tile.Mode GetType(Vector2I position)
	{
		(Tile.Mode mode, bool covered) defaultValue = (Tile.Mode.Empty, true);
		return Puzzle.State.GetValueOrDefault(position, defaultValue).mode;
	}
}

