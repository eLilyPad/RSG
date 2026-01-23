using Godot;

namespace RSG.Minesweeper;

sealed class UserInput
{
	public readonly record struct Flag(bool Placed = false);
	public readonly record struct UnCovered(Tile.Mode Type);
	public readonly record struct Nothing();

	public const MouseButton UnCoverButton = MouseButton.Left, FlagButton = MouseButton.Right;
	public required Tile.Pool Tiles { private get; init; }

	public OneOf<Flag, UnCovered, Nothing> MousePressed(Vector2I position)
	{
		Tile tile = Tiles.GetOrCreate(position);
		bool flagPressed = Input.IsMouseButtonPressed(FlagButton);
		if (flagPressed)
		{
			tile.Flagged = !tile.Flagged;
			return new Flag(Placed: tile.Flagged);
		}
		if (tile.Flagged) return new Flag(true);
		if (!tile.Covered) return new Nothing();
		tile.Covered = false;
		return new UnCovered(Type: tile.Type);
	}
}

