using Godot;

namespace RSG.Nonogram;

using static PuzzleManager;
using static Display;

public interface IReadyPuzzle { bool PuzzleReady { get; } }
public interface IManageGamePuzzle
{
	void Completed(SaveData puzzle);
}
public interface IManagePuzzle : IManageGamePuzzle
{
	void SettingsChanged();
}
public interface IAlternate { bool IsAlternative(Vector2I position); }
public interface IHavePuzzleEvents { IManagePuzzle? EventHandler { get; set; } }
public interface IHaveCurrent<T> { T Nonogram { get; } }
public sealed record class CurrentPuzzle :
	NonogramContainer.IHave,
	ITiles<CurrentPuzzle>,
	IHints<CurrentPuzzle>,
	IPuzzleTimer.IHave,
	IHavePuzzleEvents,
	Settings.IHave,
	SaveData.IHave,
	IDisplayType,
	IReadyPuzzle,
	Tile.ILocker,
	IAlternate
{
	private sealed class GameTimer<T>(T Current) : IPuzzleTimer
	where T : NonogramContainer.IHave, SaveData.IHave, IPuzzleTimer.IHave
	{
		public TimeSpan Elapsed { get; set => ChangeTime(field = value); }
		public bool Running { get; set; } = false;
		private void ChangeTime(TimeSpan value)
		{
			Current.Puzzle.TimeTaken = Current.Timer?.Elapsed ?? TimeSpan.Zero;
			Current.SetTimeText(time: value);
		}
	}
	private sealed class PuzzleHints : NodePool<HintPosition, Hint, CurrentPuzzle>, Tile.ISize
	{
		public Vector2 TileSize { get; set; } = Vector2.Zero;
		public override void Refresh(HintPosition position, CurrentPuzzle config)
		{
			Assert(config.Type is Type.Game or Type.Paint);
			Hint hint = GetOrCreate(position, config);
			hint.Label.Text = config.Puzzle.CalculateHints(position);
			hint.CustomMinimumSize = TileSize;
		}
		public override Node Parent(HintPosition position, CurrentPuzzle value) => value.HintsParent(side: position.Side);
		protected override Hint Create(HintPosition position, CurrentPuzzle value) => Hint
			.Create(position, Core.DefaultColours);
	}
	private sealed class PuzzleTiles : NodePool<Vector2I, Tile, CurrentPuzzle>, Tile.ISize
	{
		public const int ChunkSize = 5;
		public Vector2 TileSize { get; set; } = Vector2.Zero;
		public override void Refresh(Vector2I position, Tile tile, CurrentPuzzle current) => current
			.SetDisplay(position)
			.SetColours(tile, position, value: Core.DefaultColours);

		public override Node Parent(Vector2I key, CurrentPuzzle current) => current.UI.Display.TilesGrid;
		protected override Tile Create(Vector2I position, CurrentPuzzle current)
		{
			Tile tile = new() { ButtonSignals = new TileConnection(position, current) };
			current
				.SetDisplay(position)
				.SetColours(tile, position, value: Core.DefaultColours);
			return tile
				.SizeFlags(horizontal: Control.SizeFlags.ExpandFill, vertical: Control.SizeFlags.ExpandFill);
		}
	}
	private sealed class TileConnection(Vector2I position, CurrentPuzzle current) : Tile.IConnectButton
	{
		public void Pressed() => current.Input(position);
		public void MouseExited() => current.SetTileHovering(position, false);
		public void MouseEntered() => current
			.SetTileHovering(position, true)
			.Input(position);
	}
	private sealed class DataConnection<T>(T Current) : SaveData.IEvents
	where T :
		IHints<T>,
		ITiles<T>,
		NonogramContainer.IHave,
		IDisplayType,
		Settings.IHave,
		IPuzzleTimer.IHave,
		IHavePuzzleEvents,
		SaveData.IHave,
		Tile.ILocker,
		IAlternate
	{
		public void Changed(Vector2I position)
		{
			TileMode currentMode = Current.Puzzle.States.GetValueOrDefault(position);
			Current.SetDisplay(position);
			switch (Current.Type)
			{
				case Type.Game:
					Current.Timer.TryRun(currentMode);
					break;
				case Type.Paint:
					Current.Hints.Refresh(Current);
					break;
			}
		}

		public void Completed() => Current.EventHandler?.Completed(Current.Puzzle);
	}
	public static CurrentPuzzle Create(Node parent)
	{
		CurrentPuzzle current = new();
		parent.AddChild(current.UI);
		return current;
	}
	public IPuzzleTimer Timer => field ??= new GameTimer<CurrentPuzzle>(Current: this);
	public NonogramContainer UI { get; init; } = new NonogramContainer { Name = "Nonogram", Visible = false }
		.Preset(Control.LayoutPreset.FullRect);
	public NodePool<Vector2I, Tile, CurrentPuzzle> Tiles => field ??= new PuzzleTiles();
	public NodePool<HintPosition, Hint, CurrentPuzzle> Hints => field ?? new PuzzleHints();
	public IManagePuzzle? EventHandler { get; set; }
	public bool PuzzleReady => !Puzzle.Expected.IsEmpty;
	public Type Type { get; set => this.ChangeType(ref field, value); } = Type.Game;
	public Settings Settings { get; set => Set(ref field, value); } = new();
	public SaveData Puzzle { get; set => Set(ref field, value); } = new();
	public IImmutableList<Func<Vector2I, bool>> Rules => field ??= [
		(position) => Type is Type.Game && Settings.LockCompletedFilledTiles && Puzzle.IsCorrectlyFilled(position),
		(position) => Type is Type.Game && Settings.LockCompletedBlockedTiles && Puzzle.IsCorrectlyBlocked(position),
	];

	private SaveData.IEvents SaveEvents => field ??= new DataConnection<CurrentPuzzle>(this);

	private CurrentPuzzle() { }
	public bool IsAlternative(Vector2I position)
	{
		return (position.X / PuzzleTiles.ChunkSize + position.Y / PuzzleTiles.ChunkSize) % 2 == 0;
	}

	private void Set(ref Settings field, Settings value)
	{
		field = value;
		EventHandler?.SettingsChanged();
	}
	private void Set(ref SaveData field, SaveData value)
	{
		field.Disconnect(SaveEvents);
		field = Save(value)
			.ConnectTo(SaveEvents)
			.DisplayTimer(config: this)
			.DisplayPuzzle(config: this);
	}
}

