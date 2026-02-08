using Godot;

namespace RSG.Nonogram;

using Mode = Display.TileMode;
using static SaveData;

public static class SaveDataExtensions
{
	const Mode defaultValue = Mode.NULL;
	public static void DisplayPuzzle<TConfig, TTiles, THints>(this TConfig config)
	where THints : NodePool<Display.HintPosition, Hint, TConfig>, Tile.ISize
	where TTiles : NodePool<Vector2I, Tile, TConfig>, Tile.ISize
	where TConfig : IDisplayPools<TTiles, THints>, ICurrentPuzzle, NonogramContainer.IHave
	{
		TTiles tiles = config.Tiles;
		THints hints = config.Hints;
		Display display = config.UI.Display;
		int length = config.Puzzle.Size;

		IEnumerable<Vector2I> tileKeys = (Vector2I.One * length).GridRange();
		IEnumerable<Display.HintPosition> hintKeys = Display.HintPosition.AsRange(length);

		tiles.ReplaceAll<Vector2I, Tile, TConfig, TTiles>(config, tileKeys);

		display.HintsParent(Display.Side.Row).RemoveChildren(true);
		display.HintsParent(Display.Side.Column).RemoveChildren(true);

		hints.ReplaceAll<Display.HintPosition, Hint, TConfig, THints>(config, hintKeys);

		display.TilesGrid.CustomMinimumSize = Mathf.CeilToInt(length) * tiles.TileSize;
		display.TilesGrid.Columns = length;
	}
	public static Action<Vector2I, Tile> Input<TCurrent, TTiles, THints>(
		this TCurrent current,
		TTiles tiles,
		THints hints
	)
		where TTiles : NodePool<Vector2I, Tile, TCurrent>, IRefresh<Vector2I, TCurrent>
		where THints : NodePool<Display.HintPosition, Hint, TCurrent>, IRefresh<Display.HintPosition, TCurrent>, Tile.ISize
		where TCurrent : ICurrentPuzzle, IDisplayPools<TTiles, THints>
	{
		return (position, tile) =>
		{
			SaveData save = current.Puzzle;
			Settings settings = current.Settings;
			IPuzzleTimer timer = current.Timer;
			IManagePuzzle? events = current.EventHandler;

			if (!TryProcessInput(position, tile, out Mode input)) return;
			if (current.Type is PuzzleManager.Type.Game && tile.Locked) return;

			HandleInput(save, position, tile, input);
			switch (current.Type)
			{
				case PuzzleManager.Type.Game:
					if (input is Mode.Filled) timer.TryRun();
					if (current.ShouldLock(position)) tile.Locked = true;
					if (save.IsComplete) events?.Completed(save);
					if (settings.LineCompleteBlockRest)
					{
						BlockCompletedLine<TCurrent, TTiles, THints>(current, side: Display.Side.Row, position);
						BlockCompletedLine<TCurrent, TTiles, THints>(current, side: Display.Side.Column, position);
					}
					break;
				case PuzzleManager.Type.Paint:
					hints.Refresh<Display.HintPosition, Hint, TCurrent, THints>(current);
					break;
			}
			PuzzleManager.Save(save);
		};
		bool TryProcessInput(Vector2I position, Tile tile, out Mode input)
		{
			IImmutableDictionary<Vector2I, Mode> saved = current.Puzzle.States;
			input = Display.PressedMode;
			if (input is defaultValue) return false;

			Assert(saved.ContainsKey(position), $"No current tile in the data");
			Assert(tile.Mode == saved[position], "tiles displayed mode is unsynchronized from data");

			input = input == saved[position] ? Mode.Clear : input;

			return !Mode.Clear.AllEqual(saved[position], input);
		}
	}

}
