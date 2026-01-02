using Godot;

namespace RSG.Nonogram;

using static PuzzleData;
using static PuzzleManager;
using static Display;

public static class PuzzleExtensions
{
	internal static void UpdateView(this Display display, SaveData save, Tiles tiles, Hints hints)
	{
		IEnumerable<HintPosition> hintValues = save.HintKeys;
		IEnumerable<Vector2I> tileValues = save.TileKeys;
		bool firstTile = true;
		foreach (Vector2I position in tileValues)
		{
			Tile tile = tiles.GetOrCreate(position);
			TileMode state = save.States.GetValueOrDefault(key: position, defaultValue: TileMode.NULL);

			if (firstTile)
			{
				hints.TileSize = tile.Size;
				firstTile = false;
			}

			tiles.ChangeMode(position, tile, input: state);
		}
		foreach (HintPosition position in hintValues)
		{
			Hint hint = hints.GetOrCreate(position);
			hints.ApplyText(position, hint);
		}

		tiles.Clear(exceptions: tileValues);
		hints.Clear(exceptions: hintValues);

		display.TilesGrid.CustomMinimumSize = Mathf.CeilToInt(save.Scale) * hints.TileSize;
		display.ResetTheme();
	}
}

class HintsProvider(CurrentPuzzle current) : Hints.IProvider
{
	public Node Parent(HintPosition position)
	{
		Display display = current.UI.Display;
		return position.Side switch { Side.Row => display.Rows, Side.Column => display.Columns, _ => display };
	}
	public string Text(HintPosition position) => current.Type switch
	{
		Type.Paint => current.Puzzle.States.CalculateHints(position),
		_ => current.Puzzle.Expected.States.CalculateHints(position)
	};

}
class TilesProvider(CurrentPuzzle current) : Tiles.IProvider
{
	public TileMode CurrentMode(Vector2I position)
	{
		return current.Puzzle.States.GetValueOrDefault(key: position, defaultValue: TileMode.NULL);
	}
	public bool Locked(Vector2I position)
	{
		TileMode state = current.Puzzle.States.GetValueOrDefault(key: position, defaultValue: TileMode.NULL);
		TileMode expected = current.Puzzle.Expected.States.GetValueOrDefault(key: position, defaultValue: TileMode.NULL);
		return current.Settings.LineCompleteLock && state == expected;
	}
	public void Activate(Vector2I position, Tile tile)
	{
		SaveData save = current.Puzzle;
		TileMode input = PressedMode.Change(tile.Button.Text.FromText());
		//TileMode input = PressedMode switch
		//{
		//	TileMode mode when mode == CurrentMode(position) => TileMode.Clear,
		//	TileMode mode => mode
		//};
		if (input is TileMode.NULL) return;
		switch (current.Type)
		{
			case Type.Game:
				current.ChangeState(position, mode: input, tile);
				input.PlayAudio();
				if (current.Settings.LineCompleteBlockRest)
				{
					foreach (Side side in stackalloc[] { Side.Row, Side.Column })
					{
						if (!save.IsLineComplete(position, side)) { continue; }
						var line = save.States.AllInLine(position, side);
						foreach ((Vector2I coord, TileMode mode) in line)
						{
							if (mode is TileMode.Filled) { continue; }
							current.ChangeState(position: coord, mode: TileMode.Blocked);
						}
					}
				}
				if (current.Settings.HaveTimer && input is TileMode.Filled && !current.Timer.Running)
				{
					current.Timer.Running = true;
				}
				if (save is { IsComplete: true })
				{
					//Instance.PuzzlesCompleted[PuzzleManager.Current.Puzzle.Name] = true;
					current.UI.CompletionScreen.Show();
					if (save.Expected.DialogueName is string dialogueName)
					{
						Dialogues.Start(dialogueName, true);
					}

				}
				break;
			case Type.Paint:
				//tile.Button.Text = input == previousMode ? EmptyText : input.AsText();
				//foreach (HintPosition hintPosition in HintPosition.Convert(position))
				//{
				//	if (!Hints.TryGetValue(hintPosition, out Hint? hint)) { continue; }
				//	string hints = Tiles.CalculateHints(hintPosition);
				//	hint.Label.Text = hintPosition.Side is Side.Row ? hints + " " : hints;
				//}
				break;
			default: break;
		}
		Save(save);
	}

	public Node Parent() => current.UI.Display.TilesGrid;
	public string Text(Vector2I position) => current.Puzzle.States.AsText(position);
}
public sealed record class CurrentPuzzle
{

	public Type Type { get; set => UI.Display.Name = (field = value).AsName(); } = Type.Display;
	public Settings Settings { get; set; } = new Settings();
	public PuzzleTimer Timer { get; }
	public SaveData Puzzle
	{
		internal get; set
		{
			if (value is null) { return; }
			Display display = UI.Display;
			Instance.AddPuzzle(field = value);
			Timer.Elapsed = field.TimeTaken;

			display.UpdateView(save: field, tiles: _tiles, hints: _hints);
		}
	} = new SaveData();

	public NonogramContainer UI { get; }
	private readonly Tiles _tiles;
	private readonly Hints _hints;
	private readonly Dictionary<Vector2I, bool> _lockedTiles = [];
	internal CurrentPuzzle()
	{
		IColours colours = Core.Colours;
		UI = new NonogramContainer { Name = "Nonogram" }
			.SizeFlags(horizontal: Control.SizeFlags.ExpandFill, vertical: Control.SizeFlags.ExpandFill);

		_tiles = new(Provider: new TilesProvider(this), Colours: colours);
		_hints = new(Provider: new HintsProvider(this), Colours: colours);
		Timer = new()
		{
			TimeChanged = text =>
			{
				Puzzle.TimeTaken = Timer?.Elapsed ?? TimeSpan.Zero;
				UI.Display.Timer.Time.Text = text;
			}
		};
	}

	public void WhenCodeLoaderEntered(string value)
	{
		RichTextLabel validation = UI.ToolsBar.CodeLoader.Control.Validation;
		//Load(value).Switch(
		//	data => Puzzle = data as SaveData,
		//	error => validation.Text = error.Message,
		//	notFound => GD.Print("Not Found")
		//);
	}
	public void WhenCodeLoaderEdited(string value)
	{
		RichTextLabel validation = UI.ToolsBar.CodeLoader.Control.Validation;
		Code.Encode(value).Switch(
			error => validation.Text = error.Message,
			code => validation.Text = $"valid code of size: {code.Size}"
		);
	}

	internal void ChangeState(Vector2I position, TileMode mode, Tile? tile = null)
	{
		Puzzle.ChangeState(position, mode);
		_tiles.ChangeMode(position, tile ?? _tiles.GetOrCreate(position), input: mode);
	}
}