using GameTools;
using Godot;


namespace RSG.Nonogram;

using static PuzzleManager;
using static CurrentPuzzle;
using static Display;

public interface ICurrentData : IPuzzleTimer.IHave, Settings.IHave, SaveData.IHave;
public interface IDisplay : IDisplayType, NonogramContainer.IHave;
public interface ICurrentPuzzle : IDisplay, ICurrentData, Tile.ILocker, IHavePuzzleEvents;
public interface IDisplayType { Type Type { get; set; } }
public interface IReadyPuzzle { bool PuzzleReady { get; } }
public interface IManagePuzzle
{
	void Completed(SaveData puzzle);
	void SettingsChanged();
}
public interface IHavePuzzleEvents { IManagePuzzle? EventHandler { get; set; } }
public sealed record class CurrentPuzzle :
	ICurrentPuzzle,
	IReadyPuzzle,
	Tile.ILocker,
	IDisplayPools<PuzzleTiles, PuzzleHints>
{
	public sealed class GameTimer<T>(T Current) : IPuzzleTimer
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
	public sealed class PuzzleHints : NodePool<HintPosition, Hint, CurrentPuzzle>, Tile.ISize
	{
		public Vector2 TileSize { get; set; } = Vector2.Zero;
		public override void Refresh(HintPosition position, CurrentPuzzle config)
		{
			Assert(config.Type is Type.Game or Type.Paint);
			Hint hint = GetOrCreate(position, config);
			hint.Label.Text = config.Type switch
			{
				Type.Game => config.Puzzle.Expected.States.CalculateHints(position),
				Type.Paint => config.Puzzle.States.CalculateHints(position),
				_ => Hint.Empty
			};
			hint.CustomMinimumSize = TileSize;
		}
		public override Node Parent(HintPosition position, CurrentPuzzle value) => value.UI.Display
			.HintsParent(side: position.Side);
		protected override Hint Create(HintPosition position, CurrentPuzzle value) => Hint
			.Create(position, Core.Colours);
	}
	public sealed class PuzzleTiles : NodePool<Vector2I, Tile, CurrentPuzzle>, Tile.ISize
	{
		public const int ChunkSize = 5;
		const TileMode defaultValue = TileMode.Clear;
		public Vector2 TileSize { get; set; } = Vector2.Zero;
		public override void Refresh(Vector2I position, CurrentPuzzle current)
		{
			Tile.ILocker locker = current;
			Tile tile = GetOrCreate(position, current);
			tile.Mode = current.Puzzle.States.GetValueOrDefault(position, defaultValue);
			tile.IsAlternative = (position.X / ChunkSize + position.Y / ChunkSize) % 2 == 0;
			tile.Locked = locker.ShouldLock(position);
			tile.CustomMinimumSize = TileSize = tile.Size;
		}
		public override Node Parent(Vector2I key, CurrentPuzzle value) => value.UI.Display.TilesGrid;
		protected override Tile Create(Vector2I position, CurrentPuzzle value)
		{
			return Tile.CreatePooled(
				position,
				colours: Core.Colours,
				hover: HoverTile,
				activate: value.Input(tiles: value.Tiles, hints: value.Hints)
			);
			void HoverTile(bool hovering)
			{
				var tiles = _nodes.AllInLines(position);
				foreach ((Vector2I _, Tile tile) in tiles) tile.Hovering = hovering;
			}
		}
	}
	public IPuzzleTimer Timer => field ??= new GameTimer<CurrentPuzzle>(Current: this);
	public NonogramContainer UI { get; init; } = new NonogramContainer { Name = "Nonogram", Visible = false }
		.Preset(Control.LayoutPreset.FullRect);
	public PuzzleTiles Tiles => field ??= new();
	public PuzzleHints Hints => field ?? new();
	public IManagePuzzle? EventHandler { get; set; }
	public bool PuzzleReady => !Puzzle.Expected.IsEmpty;
	public Type Type { get; set => ChangeType(value: field = value); } = Type.Game;
	public Settings Settings { get; set => ChangeSettings(value: field = value); } = new();
	public SaveData Puzzle { get; set => ChangePuzzle(value: field = value); } = new();

	public IImmutableList<Func<Vector2I, bool>> Rules => field ??= [
		(position) => Settings.LockCompletedFilledTiles && Puzzle.IsCorrectlyFilled(position),
		(position) => Settings.LockCompletedBlockedTiles && Puzzle.IsCorrectlyBlocked(position),
	];

	public CurrentPuzzle() { }
	private void ChangeSettings(Settings value) => EventHandler?.SettingsChanged();
	private void ChangeType(Type value)
	{
		UI.Display.Name = value.AsName();
		UI.Display.Spacer.ChangeType(value);
	}
	private void ChangePuzzle(SaveData value)
	{
		Save(value);
		Timer.Elapsed = value.TimeTaken;
		this.DisplayPuzzle<CurrentPuzzle, PuzzleTiles, PuzzleHints>();
	}
}

