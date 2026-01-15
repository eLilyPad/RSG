using Godot;

namespace RSG.MineSweeper;

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
public sealed partial class MineSweeper : Resource, Tile.IProvider
{
	public sealed class CurrentPuzzle
	{
		public Data Puzzle
		{
			get; set
			{
				UI.Tiles.Update(value.Size);
				UI.Display.TilesGrid.Columns = value.Size;
				UI.Display.TilesGrid.CustomMinimumSize = value.Size * UI.Tiles.TileSize;
				field = value;
			}
		} = Data.CreateRandom();
		public required MineSweeperContainer UI { get; init; }

	}
	public sealed class Data
	{
		public static Data CreateRandom()
		{
			Dictionary<Vector2I, (Tile.Mode mode, bool covered)> state = [];
			foreach (Vector2I position in (5 * Vector2I.One).GridRange())
			{
				Tile.Mode mode = Random.Shared.Next(10) > 1 ? Tile.Mode.Empty : Tile.Mode.Bomb;
				bool covered = true;
				state[position] = (mode, covered);
			}
			Data data = new(state: state);
			return data;
		}
		public const int DefaultSize = 15;

		public int Size => (int)Mathf.Sqrt(_state.Count);
		public IImmutableDictionary<Vector2I, (Tile.Mode mode, bool covered)> State => _state.ToImmutableDictionary();
		private readonly Dictionary<Vector2I, (Tile.Mode mode, bool covered)> _state = [];

		private Data(Dictionary<Vector2I, (Tile.Mode mode, bool covered)> state) => _state = state;
	}
	public const string MineSweeperPath = "res://Data/MineSweeper.tres";
	public static MineSweeper Instance => field ??= MineSweeperPath.LoadOrCreateResource<MineSweeper>();

	public CurrentPuzzle Current => field ??= new() { UI = new(this, Core.Colours) };

	public void OnActivate(Vector2I position, Tile tile) => tile.Covered = false;
	public Tile.Mode GetType(Vector2I position)
	{
		Assert(Current.Puzzle.State.ContainsKey(position));
		return Current.Puzzle.State[position].mode;
	}
}

