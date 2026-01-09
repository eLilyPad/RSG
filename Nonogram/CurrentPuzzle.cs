using Godot;

namespace RSG.Nonogram;

using static Display;
using static PuzzleManager;

public interface IHavePuzzleSettings { Settings Settings { get; } }

public abstract record class CurrentPuzzle : Hints.IProvider, Tile.IProvider, PuzzleTimer.IProvider, SaveData.ISaveEvents
{
	private sealed class CompletedLinesBlocker : SaveData.Modifier
	{
		public required Tile.Pool Tiles { private get; init; }
		public override bool ShouldModify => Settings.LineCompleteBlockRest;

		protected override void ModifySave(Vector2I position)
		{
			BlockCompletedLine(position, side: Side.Row);
			BlockCompletedLine(position, side: Side.Column);
		}
		private void BlockCompletedLine(Vector2I position, Side side)
		{
			if (!Save.IsLineComplete(position, side)) { return; }
			Save.BlockLine(position, side);
		}
	}
	private sealed class UserInput : SaveData.Modifier
	{
		public required PuzzleTimer Timer { private get; init; }
		public required Tile.Pool Tiles { private get; init; }
		public required Type Type { private get; init; }
		public override bool ShouldModify => Type is Type.Game;

		protected override void ModifySave(Vector2I position)
		{
			const TileMode defaultValue = TileMode.NULL;

			TileMode input = PressedMode;
			if (input is defaultValue) return;

			Assert(Save.States.ContainsKey(position), $"No current tile in the data");
			input = input == Save.States[position] ? TileMode.Clear : input;
			if (Tiles.GetOrCreate(position).Locked) return;

			input.PlayAudio();
			ChangeState(position, mode: input);

			if (!Timer.Running && input is TileMode.Filled) Timer.Running = true;
		}
	}

	public IHaveEvents? EventHandler { get; set; }
	public Type Type { get; set => UI.Display.Name = (field = value).AsName(); } = Type.Display;
	public Settings Settings { get; set => ChangeSettings(field = value); } = new Settings();
	public PuzzleTimer Timer { get; }
	public string CompletionDialogueName => Puzzle.Expected.DialogueName;
	public SaveData Puzzle
	{
		private get; set
		{
			Assert(value is not null, "null save puzzle was given to the Puzzle manager. Unable to change puzzle");
			Display display = UI.Display;
			Tile.Pool tiles = UI.Tiles;
			Hints hints = UI.Hints;
			int size = display.TilesGrid.Columns = value.Size;

			Instance.AddSave(value);
			Player.Save = Completer.Save = value;
			Timer.Elapsed = value.TimeTaken;

			tiles.Update(size);
			hints.TileSize = tiles.TileSize;
			hints.Update(size);
			display.ResetTheme();
			display.TilesGrid.CustomMinimumSize = Mathf.CeilToInt(value.Size) * tiles.TileSize;

			value.Events = this;
			field = value;


		}
	} = new SaveData();

	public NonogramContainer UI { get; private set; }

	private CompletedLinesBlocker Completer { get; init; }
	private UserInput Player { get; init; }

	internal CurrentPuzzle()
	{
		ColourPack colours = Core.Colours;
		List<Func<Vector2I, bool>> rules = [
			(position) => Settings.LockCompletedFilledTiles && Puzzle.IsCorrectlyFilled(position),
			(position) => Settings.LockCompletedBlockedTiles && Puzzle.IsCorrectlyBlocked(position),
		];

		UI = new NonogramContainer(
			tiles: new(Provider: this, Colours: colours) { LockRules = new() { Rules = rules } },
			hints: new(Provider: this, Colours: colours)
		)
		{ Name = "Nonogram", }
			.SizeFlags(horizontal: Control.SizeFlags.ExpandFill, vertical: Control.SizeFlags.ExpandFill);
		Timer = new() { Provider = this };
		Completer = new()
		{
			Save = Puzzle,
			Tiles = UI.Tiles,
			Settings = Settings,
		};
		Player = new()
		{
			Save = Puzzle,
			Settings = Settings,
			Timer = Timer,
			Tiles = UI.Tiles,
			Type = Type
		};
	}

	public void TileChanged(Vector2I position, TileMode mode)
	{
		Tile.Pool tiles = UI.Tiles;
		Tile tile = tiles.GetOrCreate(position);
		tile.Mode = mode;
		tile.Locked = tiles.LockRules.ShouldLock(position);
		Completer.Modify(position);
	}
	public void Completed() => EventHandler?.Completed(Puzzle);
	void PuzzleTimer.IProvider.TimeChanged(string value)
	{
		Puzzle.TimeTaken = Timer?.Elapsed ?? TimeSpan.Zero;
		UI.Display.Timer.Time.Text = "[font_size=30]" + value;
	}
	Node Hints.IProvider.Parent(HintPosition position) => UI.Display.HintsParent(side: position.Side);
	string Hints.IProvider.Text(HintPosition position) => Puzzle.Expected.States.CalculateHints(position);
	Node Tile.IProvider.Parent() => UI.Display.TilesGrid;
	TileMode Tile.IProvider.State(Vector2I position)
	{
		return Puzzle.States.GetValueOrDefault(position, TileMode.Clear);
	}
	void Tile.IProvider.OnActivate(Vector2I position, Tile tile)
	{
		Player.Modify(position);
		Save(Puzzle);
	}

	private void ChangeSettings(Settings value)
	{
		Player.Settings = value;
		EventHandler?.SettingsChanged();
	}
}
