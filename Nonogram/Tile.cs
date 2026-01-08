using Godot;

namespace RSG.Nonogram;

using static Display;

public sealed partial class Tile : PanelContainer
{
	public sealed class Locker
	{
		private readonly List<Func<Vector2I, bool>> _rules = [];
		public Locker(PuzzleManager.CurrentPuzzle puzzle)
		{
			_rules.Add((position) => puzzle.Settings.LockCompletedFilledTiles && puzzle.Puzzle.IsCorrectlyFilled(position));
			_rules.Add((position) => puzzle.Settings.LockCompletedBlockTiles && puzzle.Puzzle.IsCorrectlyBlocked(position));
		}
		public bool ShouldLock(Vector2I position) => _rules.Any(rule => rule(position));
	}
	internal interface IProvider
	{
		Node Parent();
		void OnActivate(Vector2I position, Tile tile);
	}
	internal sealed class Pool(IProvider Provider, IColours Colours) : NodePool<Vector2I, Tile>
	{
		public override void Clear(IEnumerable<Vector2I> exceptions) => Clear(_ => Provider.Parent(), exceptions);
		protected override Tile Create(Vector2I position)
		{
			const int chunkSize = 5;
			Tile tile = new Tile
			{
				Name = $"Tile (X: {position.X}, Y: {position.Y})",
				IsAlternative = (position.X / chunkSize + position.Y / chunkSize) % 2 == 0,
				Colours = Colours,
				Mode = TileMode.Clear,
			}.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
			Provider.Parent().AddChild(tile);

			tile.Resized += () => tile.Button.PivotOffset = tile.Button.Size / 2;
			tile.Button.ButtonDown += () => Provider.OnActivate(position, tile);
			tile.Button.MouseExited += () => HoverTile(false);
			tile.Button.MouseEntered += () =>
			{
				Provider.OnActivate(position, tile);
				HoverTile(true);
			};

			tile.Style.CornerDetail = 1;
			tile.Style.SetCornerRadiusAll(0);
			tile.Button.AddThemeStyleboxOverride(themeName, tile.Style);

			tile.Button.AddAllFontThemeOverride(Colors.Transparent);
			tile.Button.AddThemeFontSizeOverride("font_size", 10);

			return tile;

			void HoverTile(bool hovering)
			{
				var tiles = _nodes.AllInLines(position);
				foreach ((Vector2I _, Tile tile) in tiles) tile.Hovering = hovering;
			}
		}
	}
	private const string themeName = "normal";
	private const MouseButtonMask mask = MouseButtonMask.Left | MouseButtonMask.Right;

	public Button Button { get; } = new Button { Text = EmptyText, ButtonMask = mask }
		.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);

	public bool IsAlternative { get; private init; } = false;
	[Export] public bool Locked { get; set => ChangeLocked(field = value); } = false;
	[Export] public bool Hovering { get; set => ChangeHovering(field = value); } = false;
	[Export] public TileMode Mode { get; set => ChangeMode(field = value); } = TileMode.NULL;

	public required IColours Colours
	{
		private get; set
		{
			if (value is null) return;
			field = value;
		}
	}

	private StyleBoxFlat Style => field ??= Button.GetThemeStylebox(themeName).Duplicate() as StyleBoxFlat
		?? throw new Exception("No theme style was obtained for");

	private Tile() { }
	public override void _Ready() => this.Add(Button);

	private void ChangeHovering(bool value) => Button.Scale = Vector2.One * (value ? .9f : 1);
	private void ChangeLocked(bool value)
	{
		Style.SetBorderWidthAll(value ? 2 : 0);
		Button.AddThemeStyleboxOverride(themeName, Style);
	}
	private void ChangeMode(TileMode value)
	{
		Style.BgColor = Background(value);
		Style.BorderColor = LockedBorder(value);
		Button.AddThemeStyleboxOverride(themeName, Style);
	}
	private Color LockedBorder(TileMode value)
	{
		Color filled = Colours.NonogramFilledBorder;
		Color blocked = Colours.NonogramBlockedBorder;
		return value switch { TileMode.Filled => filled, _ => blocked };
	}

	private Color Background(TileMode value)
	{
		Color
		filled = Colours.NonogramTileBackgroundFilled,
		background = IsAlternative ? Colours.NonogramTileBackground1 : Colours.NonogramTileBackground2,
		blocked = background.Darkened(.2f);

		filled = IsAlternative ? filled : filled.Darkened(.2f);

		return value switch { TileMode.Filled => filled, TileMode.Blocked => blocked, _ => background };
	}
}
