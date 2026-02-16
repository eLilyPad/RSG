using Godot;

namespace RSG.Nonogram;

using Mode = Display.TileMode;
using static SaveData;

public static class SaveDataExtensions
{
	const Mode defaultValue = Mode.NULL;
	public static SaveData DisplayPuzzle<TConfig>(this SaveData save, TConfig config)
	where TConfig :
		NonogramContainer.IHave,
		IHavePuzzleEvents,
		IHints<TConfig>,
		ITiles<TConfig>
	{
		Display display = config.UI.Display;
		int length = save.Size;

		IEnumerable<Vector2I> tileKeys = (Vector2I.One * length).GridRange();
		IEnumerable<Display.HintPosition> hintKeys = Display.HintPosition.AsRange(length);

		config.Tiles.ReplaceAll(config, tileKeys);
		config.HintsParent(Display.Side.Row).RemoveChildren(true);
		config.HintsParent(Display.Side.Column).RemoveChildren(true);
		config.Hints.ReplaceAll(config, hintKeys);
		//config.Hints.Refresh(config);

		display.TilesGrid.Columns = length;
		return save;
	}
	public static void Input<TCurrent>(this TCurrent current, Vector2I position)
	where TCurrent :
		ITiles<TCurrent>,
		Settings.IHave,
		Tile.ILocker,
		IDisplayType,
		IAlternate,
		IHave
	{
		if (Display.PressedMode is Mode input && input is defaultValue) return;

		SaveData save = current.Puzzle;
		Mode currentMode = save.States.GetValueOrDefault(position);
		input = input == currentMode ? Mode.Clear : input;

		//if (Mode.Clear.AllEqual(currentMode, input)) return;

		HandleInput(current, position, input);
		//_ = current.TryLock(position);
		PuzzleManager.Save(save);
	}

}
