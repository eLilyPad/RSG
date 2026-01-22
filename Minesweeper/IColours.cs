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

